using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOBuddy
{
    /// <summary>
    /// RESUPPLY — standing in a shop (Fair Trade, Omni/Clan store), walk to the terminal that sells stims
    /// and rechargers, buy the best ones the bot can actually use, and ask the owner for credits when it
    /// can't pay. While a run is active this controller OWNS movement (Main gives it the frame ahead of
    /// travel and follow) and the decision tick is held, so nothing else sits, casts or wanders off.
    ///
    /// The shop protocol, as the live server speaks it (AO Item Assistant's logs of real sessions, and
    /// OmniCell's vendor handler for the slot numbering):
    ///   Use (GenericCmd) on the VendingMachine  -> server sends ShopUpdate: the machine's stock, and a
    ///                                              Trade Open with the machine as the partner
    ///   Trade AddItem(machine, 0x6F, slot)      -> one of the stock line's items onto the buy side; slot
    ///                                              is the 0-based index into ShopUpdate's list, and the
    ///                                              same line can be added again for another copy
    ///   Trade Accept(machine)                   -> server commits (Trade op 4 from the machine), sends each
    ///                                              item as an AddTemplate and takes the credits
    ///   Trade Decline(machine)                  -> closes the window, buys nothing
    /// A line costs Value x the terminal's SellModifier/100 x the buyer's Computer Literacy discount
    /// (ItemValues.ShopPrice). The modifier is not in the terminal's spawn packet; it's on the terminal's
    /// template (StaticInstance), which ItemValues carries - at ICC Fair Trade the Basic/Advanced/Superior
    /// pharmacies are 105/505/1005. After each purchase, paid / computed is saved per terminal to
    /// resupply.json as a correction (expected ~1.0; it's there in case another shop disagrees). A terminal
    /// whose template has no modifier is priced at bare Value and the server is the judge: a buy that doesn't
    /// go through is retried at half the count, and one that fails at a single item means we can't afford it.
    ///
    /// One item kind per purchase, so each cash drop belongs to one price.
    /// </summary>
    public class ResupplyController
    {
        private enum Phase { Idle, Approach, Opening, Adding, Settling, WaitMoney }
        private enum Supply { Stim, Recharger, Container }

        private sealed class Offer { public int Slot; public Item Item; }

        private sealed class Memory
        {
            public Dictionary<string, double> Corrections = new Dictionary<string, double>(); // "pf:instance" -> paid / computed price
            public Dictionary<string, string> Machines = new Dictionary<string, string>(); // "pf:instance" -> "Stim,Recharger" / ""
        }

        private readonly BotContext _ctx;
        private readonly Movement _move;
        private readonly string _file;
        private Memory _mem;

        private Phase _phase = Phase.Idle;
        private double _phaseTime;
        private readonly List<Identity> _candidates = new List<Identity>();
        private Identity _machine = Identity.None;
        private string _machineName = "";
        private float _bestDist;
        private double _lastProgressAt;
        private int _opens;                       // machine opens this run (runaway guard)

        // What the current machine answered with.
        private VendingMachineSlot[] _stock;
        private bool _tradeDone, _tradeDeclined;
        private string _lastFeedback;

        // The purchase in flight.
        private Supply _kind;
        private Offer _offer;
        private int _count, _added;
        private int _retryCount;                  // unknown price: the halved count to try on the reopen
        private int _cashBefore;
        private double _doneAt = -1;

        // What this run set out to do, so a purchase the inventory model never registers can't loop us.
        private readonly Dictionary<Supply, int> _haveAtStart = new Dictionary<Supply, int>();
        private readonly Dictionary<Supply, int> _bought = new Dictionary<Supply, int>();

        // Waiting on the owner for credits.
        private int _cashAtWait;
        private double _nagAccum;

        // The owner handing us credits in a player trade.
        private bool _ownerTrade;
        // Our answer to his accept, wire-proven: Accept(owner) then Confirm(owner), each naming the owner with
        // p34 empty (as AOSharp.Core sends them). One Confirm closes it; a second opens a second confirm window.
        private enum OwnerStep { None, Accept, Confirm, Done }
        private OwnerStep _ownerStep;
        private double _ownerWait;
        private const double OwnerAcceptDelay = 1.0;     // after his accept (an accept right on his change is dropped)
        private const double OwnerConfirmDelay = 1.5;    // after our accept

        private const float WalkSpeed = 9.0f;            // walk, not run, so we don't overshoot the terminal
        private const double StuckSeconds = 4.0;         // no closer in this long = can't reach it, next machine
        private const double OpenTimeout = 5.0;          // no ShopUpdate in this long = not a shop / too far
        private const double FirstAddDelay = 0.5;        // let the shop window open before adding to it
        private const double AddInterval = 0.2;          // one AddItem per this, like a player clicking
        private const double SettleTimeout = 8.0;        // no commit in this long = the server refused the buy
        private const double CashSettle = 1.5;           // after the commit, wait this long for the Cash stat
        private const int MaxOpens = 16;

        // TEMPORARY — 'vendordebug': open every matching terminal in the zone just to log what it says
        // (VENDMACHINE / SHOPUPDATE lines), buying nothing. Remove once the price multipliers are understood.
        private bool _survey;
        private int _surveyTotal, _surveyLogged;

        public bool Active => _phase != Phase.Idle;

        public ResupplyController(BotContext ctx, Movement move, string pluginDir)
        {
            _ctx = ctx;
            _move = move;
            _file = Path.Combine(pluginDir, "resupply.json");
        }

        // Below the warning floor on either — what solo mode will check before deciding to go shopping.
        public bool NeedsResupply()
            => Have(Supply.Stim) <= _ctx.Config.LowStimCount || Have(Supply.Recharger) <= _ctx.Config.LowRechargerCount;

        // ---- Commands -----------------------------------------------------------

        public void Start(LocalPlayer me, Action<string> reply)
        {
            if (Active) { reply("Already resupplying — " + Describe()); return; }
            EnsureLoaded();

            _containersWanted = 0;
            _haveAtStart.Clear(); _bought.Clear();
            foreach (Supply s in new[] { Supply.Stim, Supply.Recharger }) { _haveAtStart[s] = Have(s); _bought[s] = 0; }
            List<Supply> needs = Needs();
            if (needs.Count == 0)
            {
                reply($"Stocked up: {Have(Supply.Stim)} stims, {Have(Supply.Recharger)} rechargers. Nothing to buy.");
                return;
            }

            BuildCandidates(me, needs);
            if (_candidates.Count == 0)
            {
                reply($"No shop terminals within {_ctx.Config.ResupplySearchRadius:0}m that could sell {string.Join(" or ", needs.Select(Plural))}.");
                return;
            }

            _opens = 0; _retryCount = 0;
            me.TryGetStat(Stat.Cash, out int cash);
            reply($"Resupplying: {string.Join(", ", needs.Select(s => $"{Remaining(s)} {Plural(s)}"))}. {cash} credits, {_candidates.Count} terminal(s) to check.");
            _ctx.Log($"RESUPPLY: start — needs {string.Join(", ", needs.Select(s => $"{s} have {Have(s)} want {Want(s)}"))}, cash {cash}, candidates {_candidates.Count}.");
            NextMachine(me);
        }

        // CONTAINERS (mission run housekeeping, 2026-09-24): buy `count` bags - the cheapest, by exact name
        // (ResupplyContainerName) - and nothing else. Same terminals and trade protocol as stims; the owner's
        // client bought a Large Backpack this way (capture 20260923-234203: Use, Trade AddItem line 1, Trade End).
        public void StartContainers(LocalPlayer me, int count, Action<string> reply)
        {
            if (Active) { reply("Already shopping - " + Describe()); return; }
            EnsureLoaded();
            _containersWanted = Math.Max(1, count);
            _haveAtStart.Clear(); _bought.Clear();
            foreach (Supply s in new[] { Supply.Stim, Supply.Recharger, Supply.Container }) { _haveAtStart[s] = Have(s); _bought[s] = 0; }
            var needs = Needs();
            BuildCandidates(me, needs);
            if (_candidates.Count == 0) { _containersWanted = 0; reply($"No shop terminals within {_ctx.Config.ResupplySearchRadius:0}m that could sell bags."); return; }
            _opens = 0; _retryCount = 0;
            me.TryGetStat(Stat.Cash, out int cash);
            reply($"Buying {_containersWanted} {Name(Supply.Container)}(s). {cash} credits, {_candidates.Count} terminal(s) to check.");
            _ctx.Log($"RESUPPLY: start - containers {_containersWanted}, cash {cash}, candidates {_candidates.Count}.");
            NextMachine(me);
        }
        private int _containersWanted;
        public bool BuyingContainers => Active && _containersWanted > 0;

        // TEMPORARY (see _survey). Every terminal in the zone whose name contains the filter, visited
        // nearest-next from where the bot stands.
        public void StartSurvey(LocalPlayer me, string filter, Action<string> reply)
        {
            if (Active) { reply("Busy — " + Describe()); return; }
            EnsureLoaded();
            if (string.IsNullOrEmpty(filter)) filter = "pharma";
            var left = DynelManager.VendingMachines
                .Where(vm => vm.Name != null && vm.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if (left.Count == 0)
            {
                int all = DynelManager.VendingMachines.Count;
                reply($"No terminals matching '{filter}' in this zone ({all} terminal(s) here in total; 'resupply machines' lists the near ones).");
                return;
            }

            _candidates.Clear();
            Vector3 at = me.MovementComponent.Position;
            while (left.Count > 0)
            {
                VendingMachine next = left.OrderBy(vm => Vector3.Distance(at, vm.Transform.Position)).First();
                _candidates.Add(next.Identity);
                at = next.Transform.Position;
                left.Remove(next);
            }

            _survey = true; _surveyTotal = _candidates.Count; _surveyLogged = 0; _opens = 0;
            _ctx.Log($"VENDORDEBUG: surveying {_surveyTotal} terminal(s) matching '{filter}'.");
            reply($"Vendor debug: opening {_surveyTotal} terminal(s) matching '{filter}' and logging each one. 'resupply stop' ends it.");
            NextMachine(me);
        }

        public void Stop(LocalPlayer me, string why)
        {
            if (!Active) return;
            if (_phase == Phase.Opening || _phase == Phase.Adding || _phase == Phase.Settling) CloseShop();
            _ctx.Log($"RESUPPLY: stopped ({why}).");
            if (me != null) _move.Stop(me, _ctx.Config.SendIntervalMs);
            Reset();
        }

        public void Forget()
        {
            EnsureLoaded();
            _mem.Machines.Clear();
            Save();
        }

        public string Describe()
        {
            string what = _offer != null ? $" {_added}/{_count}x {_offer.Item.Name} QL{_offer.Item.Ql}" : "";
            switch (_phase)
            {
                case Phase.Idle: return $"idle. {Have(Supply.Stim)} stims, {Have(Supply.Recharger)} rechargers; {(_mem?.Corrections.Count ?? 0)} terminal price correction(s) learned.";
                case Phase.WaitMoney: return $"waiting for credits at '{_machineName}' to buy{what}.";
                default: return $"{_phase} '{_machineName}'{what} ({_candidates.Count} terminal(s) left).";
            }
        }

        // ---- Wire -----------------------------------------------------------------

        public void OnMessage(Message m)
        {
            if (m?.Body == null) return;

            if (m.Body is VendingMachineFullUpdateMessage vfu) { LogMachineSpawn(vfu); return; }

            if (m.Body is ShopUpdateMessage shop)
            {
                LogShopUpdate(shop);
                if (shop.Identity == _machine) _stock = shop.VendingMachineSlots ?? new VendingMachineSlot[0];
                return;
            }

            if (m.Body is TradeMessage trade)
            {
                var partner = new Identity((IdentityType)trade.Param1, trade.Param2);
                if (Active || _ownerTrade) _ctx.Log($"RESUPPLY: Trade rx {trade.Action} partner={partner} p3={trade.Param3:X} p4={trade.Param4}");
                bool change = trade.Action == TradeAction.UpdateCredits || trade.Action == TradeAction.AddItem
                           || trade.Action == TradeAction.RemoveItem || trade.Action == TradeAction.OtherPlayerAddItem;
                // He changed the offer: his accept is void; we answer his next one afresh.
                if (_ownerTrade && change) _ownerStep = OwnerStep.None;
                // The server's Decline names no partner (None:0) - it closes whatever window is open.
                if (trade.Action == TradeAction.Decline && (partner == _machine || partner == Identity.None)) _tradeDeclined = true;
                else if (trade.Action == TradeAction.Complete && partner == _machine) _tradeDone = true;
                return;
            }

            if (!Active) return;
            if (m.Body is AddTemplateMessage add)
            {
                _ctx.Log($"RESUPPLY: AddTemplate low={add.LowId} high={add.HighId} QL{add.Quality} count={add.Count}");
                return;
            }
            if (m.Body is FormatFeedbackMessage ff) { _lastFeedback = ff.FormattedMessage; _ctx.Log($"RESUPPLY: feedback '{_lastFeedback}'"); }
            else if (m.Body is FeedbackMessage fb) { _lastFeedback = $"feedback {fb.CategoryId}/{fb.MessageId}"; _ctx.Log($"RESUPPLY: {_lastFeedback}"); }
        }

        // A player trade with the owner: he's handing us something (credits, when we asked). We put nothing
        // on our side, so accepting can only ever gain. His accept is answered with our Accept, then our Confirm.
        // The server echoes each Confirm back as Confirm(us) - that is not his, and must not be answered.
        public void OnTradeStatus(Identity who, TradeStatus status)
        {
            if (status == TradeStatus.Opened)
            {
                _ownerTrade = _ctx.OwnerCharId != 0 && Trade.CurrentTarget.Instance == _ctx.OwnerCharId;
                _ownerStep = OwnerStep.None;
                if (_ownerTrade) _ctx.Log("RESUPPLY: owner opened a trade.");
                return;
            }
            if (!_ownerTrade) return;

            switch (status)
            {
                case TradeStatus.Accept:
                    if (_ownerStep != OwnerStep.None) break;
                    _ownerStep = OwnerStep.Accept;
                    _ownerWait = 0;
                    _ctx.Log($"RESUPPLY: owner accepted the trade (offering {Trade.TargetWindowCache.Credits} credits, {Trade.TargetWindowCache.Items.Count} item(s)) — accepting.");
                    break;
                case TradeStatus.Finished:
                case TradeStatus.Declined:
                    _ctx.Log($"RESUPPLY: owner trade {status}.");
                    _ownerTrade = false;
                    _ownerStep = OwnerStep.None;
                    break;
            }
        }

        // Runs every frame, resupply or not: Accept(owner) a moment after his accept, Confirm(owner) after that.
        private void OwnerTradeTick(double dt)
        {
            if (!_ownerTrade || _ownerStep == OwnerStep.None || _ownerStep == OwnerStep.Done) return;
            _ownerWait += dt;
            if (_ownerWait < (_ownerStep == OwnerStep.Accept ? OwnerAcceptDelay : OwnerConfirmDelay)) return;
            _ownerWait = 0;

            TradeAction action = _ownerStep == OwnerStep.Accept ? TradeAction.Accept : TradeAction.Confirm;
            Identity owner = Trade.CurrentTarget;
            _ctx.Log($"RESUPPLY: owner trade — {action}.");
            Client.Send(new TradeMessage { Version = 2, Action = action, Param1 = (int)owner.Type, Param2 = owner.Instance });
            // After Confirm we're done (until he changes the offer); the server finishes once he confirms too.
            _ownerStep = _ownerStep == OwnerStep.Accept ? OwnerStep.Confirm : OwnerStep.Done;
        }

        public void OnZone() { if (Active) { Tell("Zoned while resupplying — stopped."); Stop(DynelManager.LocalPlayer, "zoned"); } }

        // ---- Frame ----------------------------------------------------------------

        // Runs every frame while a resupply is active. Returns true when it owns this frame's movement.
        public bool Tick(LocalPlayer me, double dt)
        {
            OwnerTradeTick(dt);
            if (!Active) return false;
            _phaseTime += dt;

            switch (_phase)
            {
                case Phase.Approach: ApproachTick(me, dt); break;
                case Phase.Opening: OpeningTick(me); break;
                case Phase.Adding: AddingTick(me); break;
                case Phase.Settling: SettlingTick(me); break;
                case Phase.WaitMoney: WaitMoneyTick(me, dt); break;
            }
            return true;
        }

        private void ApproachTick(LocalPlayer me, double dt)
        {
            if (!DynelManager.Find(_machine, out Dynel vm)) { _ctx.Log($"RESUPPLY: '{_machineName}' is gone."); NextMachine(me); return; }

            Vector3 pos = me.MovementComponent.Position;
            Vector3 goal = vm.Transform.Position;
            float dist = Vector3.Distance(pos, goal);

            if (dist <= _ctx.Config.ResupplyUseRange)
            {
                _move.Stop(me, _ctx.Config.SendIntervalMs);
                Open(me);
                return;
            }

            if (dist < _bestDist - 0.3f) { _bestDist = dist; _lastProgressAt = _phaseTime; }
            else if (_phaseTime - _lastProgressAt > StuckSeconds)
            {
                _ctx.Log($"RESUPPLY: can't get closer than {dist:0.0}m to '{_machineName}' — skipping it.");
                _move.Stop(me, _ctx.Config.SendIntervalMs);
                NextMachine(me);
                return;
            }

            Vector3 dir = (goal - pos).Normalize();
            float step = Movement.CappedStep(WalkSpeed, dt, _ctx.Config.MaxStep, dist - _ctx.Config.ResupplyUseRange * 0.5f);
            _ctx.WalkState = $"resupply(walk) d={dist:0.0} -> '{_machineName}'";
            _move.Advance(me, pos + dir * Math.Max(step, 0f), Movement.SafeLook(dir, me.MovementComponent.Heading), run: false, dt, _ctx.Config.SendIntervalMs);
        }

        private void OpeningTick(LocalPlayer me)
        {
            if (_stock == null)
            {
                if (_phaseTime > OpenTimeout)
                {
                    _ctx.Log($"RESUPPLY: '{_machineName}' didn't open a shop in {OpenTimeout:0}s{(_lastFeedback != null ? $" ('{_lastFeedback}')" : "")} — skipping it.");
                    NextMachine(me);
                }
                return;
            }
            if (_phaseTime < FirstAddDelay) return;

            if (_survey)
            {
                LogStock();   // the stock itself was logged as the SHOPUPDATE arrived
                _surveyLogged++;
                _ctx.Log($"VENDORDEBUG: logged '{_machineName}' {_machine} ({_surveyLogged}/{_surveyTotal}).");
                CloseShop();
                NextMachine(me);
                return;
            }

            // What does it sell? Remember it for this playfield, so the next run walks straight to the right one.
            List<Offer> offers = _stock.Select((s, i) => new Offer { Slot = i, Item = StockItem(s) })
                                       .Where(o => !string.IsNullOrEmpty(o.Item?.Name)).ToList();
            var sells = new List<Supply>();
            foreach (Supply s in new[] { Supply.Stim, Supply.Recharger, Supply.Container })
                if (offers.Any(o => Is(o.Item, s))) sells.Add(s);
            Remember(_machine, sells);
            LogStock();
            _ctx.Log($"RESUPPLY: '{_machineName}' stims/rechargers: " +
                     (sells.Count == 0 ? "none" : string.Join(", ", offers.Where(o => sells.Any(s => Is(o.Item, s))).Select(o => $"[{o.Slot}] {o.Item.Name} QL{o.Item.Ql}"))));

            foreach (Supply need in Needs())
            {
                // Fitting = the highest QL whose First Aid / Treatment requirement he meets right now.
                // A container: the cheapest line, no skill to meet.
                Offer best = need == Supply.Container
                    ? offers.Where(o => Is(o.Item, need)).OrderBy(o => PriceEach(o.Item, out _)).FirstOrDefault()
                    : offers.Where(o => Is(o.Item, need) && SupportController.MeetsHealReqs(o.Item, me))
                            .OrderByDescending(o => o.Item.Ql).FirstOrDefault();
                if (best == null)
                {
                    if (sells.Contains(need)) _ctx.Log($"RESUPPLY: '{_machineName}' has {Plural(need)} but none I have the skill for.");
                    continue;
                }
                BeginPurchase(me, need, best);
                return;
            }

            CloseShop();
            NextMachine(me);
        }

        // The machine's own stats again as we open it (where a markup would be); the stock was logged on arrival.
        private void LogStock()
        {
            if (DynelManager.Find(_machine, out VendingMachine vm) && vm.Stats != null)
                _ctx.Log($"RESUPPLY: '{_machineName}' {Modifiers(vm.Stats)} | stats: " + string.Join(" ", vm.Stats.Select(kv => $"{kv.Key}={kv.Value}")));
        }

        // ---- Packet logs ----------------------------------------------------------
        // Logged whenever they arrive, not only while resupplying: every vending machine the zone hands us
        // (with the stats a markup would live in), and every shop window's contents. This is how the
        // per-machine price multiplier gets matched to what the server actually says about the machine.

        private void LogMachineSpawn(VendingMachineFullUpdateMessage v)
        {
            var stats = new Dictionary<Stat, int>();
            foreach (var st in v.Stats ?? new GameTuple<Stat, int>[0]) stats[st.Value1] = st.Value2;
            string name = stats.TryGetValue(Stat.StaticInstance, out int tmpl) && tmpl != 0
                          && ItemData.Find(tmpl, out DummyItem d) && d.Name != null ? d.Name : "?";
            string at = v.Position.HasValue ? $"({v.Position.Value.X:0},{v.Position.Value.Y:0},{v.Position.Value.Z:0})" : $"(in {v.OwnerType:X}:{v.OwnerInstance})";
            _ctx.Log($"VENDMACHINE {v.Identity} '{name}' at {at} pf={v.PlayfieldId} {Modifiers(stats)} | " +
                     $"u1={v.Unknown1} sm={v.StateMachine} u4={v.Unknown4} u6={v.Unknown6} u7={v.Unknown7} tail={v.TailVersion} " +
                     $"arr=[{string.Join(",", v.UnknownArray ?? new int[0])}] u9={v.Unknown9} | stats: " +
                     string.Join(" ", stats.Select(kv => $"{kv.Key}={kv.Value}")));
        }

        private void LogShopUpdate(ShopUpdateMessage shop)
        {
            VendingMachineSlot[] lines = shop.VendingMachineSlots ?? new VendingMachineSlot[0];
            string name = DynelManager.Find(shop.Identity, out VendingMachine vm) && !string.IsNullOrEmpty(vm.Name) ? vm.Name : "?";
            _ctx.Log($"SHOPUPDATE {shop.Identity} '{name}' {(vm?.Stats != null ? Modifiers(vm.Stats) : "")} CL={Cl()} — {lines.Length} line(s):");
            for (int i = 0; i < lines.Length; i++)
            {
                VendingMachineSlot s = lines[i];
                Item it = StockItem(s);
                bool valued = ItemValues.TryGet(s.ItemLowId, s.ItemHighId, s.Quality, out int v);
                int price = valued ? Price(v, shop.Identity, out _) : 0;
                _ctx.Log($"SHOPUPDATE   [{i}] low={s.ItemLowId} high={s.ItemHighId} QL{s.Quality} '{it?.Name ?? "?"}' value={(valued ? v.ToString() : "?")}" +
                         (price > 0 ? $" price={price}" : ""));
            }
        }

        // The terminal's markups, from its template (StaticInstance) - its own stats don't carry them.
        private static string Modifiers(Dictionary<Stat, int> stats)
            => stats.TryGetValue(Stat.StaticInstance, out int tmpl) && ItemValues.TryGetShopModifiers(tmpl, out int sm, out int bm)
                ? $"template {tmpl} SellModifier={sm} BuyModifier={bm}"
                : $"template {(tmpl != 0 ? tmpl.ToString() : "?")} SellModifier=- BuyModifier=-";

        // For 'resupply machines': every terminal within the search radius, nearest first.
        public List<string> DescribeMachines(LocalPlayer me)
        {
            EnsureLoaded();
            Vector3 at = me.MovementComponent.Position;
            var lines = new List<string>();
            foreach (VendingMachine vm in DynelManager.VendingMachines.OrderBy(x => Vector3.Distance(at, x.Transform.Position)))
            {
                float d = Vector3.Distance(at, vm.Transform.Position);
                if (d > _ctx.Config.ResupplySearchRadius) break;
                string key = MachineKey(vm.Identity);
                string sells = _mem.Machines.TryGetValue(key, out string s) ? (s.Length == 0 ? "sells neither" : "sells " + s) : "unchecked";
                string mult = _mem.Corrections.TryGetValue(key, out double m) ? $", price correction x{m:0.#####}" : "";
                string line = $"'{vm.Name ?? "?"}' {d:0}m {Modifiers(vm.Stats ?? new Dictionary<Stat, int>())} ({sells}{mult})";
                lines.Add(line);
                _ctx.Log($"MACHINES {vm.Identity} {line} | stats: " + string.Join(" ", (vm.Stats ?? new Dictionary<Stat, int>()).Select(kv => $"{kv.Key}={kv.Value}")));
            }
            return lines;
        }

        // A stock line as an item (name, QL-interpolated requirements). A line whose low/high templates
        // don't line up in the item data throws in the SDK's interpolation; it can't be a stim we'd buy.
        private static Item StockItem(VendingMachineSlot s)
        {
            try { return new Item(s.ItemLowId, s.ItemHighId, s.Quality); }
            catch { return null; }
        }

        private void BeginPurchase(LocalPlayer me, Supply kind, Offer offer)
        {
            _kind = kind; _offer = offer;
            // A purchase lands as ONE stack (seen: 7 stims bought = one slot used), so it needs one free slot
            // beyond the reserve, not one per item.
            if (Inventory.NumFreeSlots <= _ctx.Config.ResupplyKeepFreeSlots)
            {
                CloseShop();
                Finish(me, $"My inventory is full — can't buy {Plural(kind)}.");
                return;
            }
            int count = Remaining(kind);
            if (_retryCount > 0) count = Math.Min(count, _retryCount);

            me.TryGetStat(Stat.Cash, out int cash);
            int price = PriceEach(offer.Item, out string priceFrom);
            if (price > 0)
            {
                int affordable = (cash - _ctx.Config.ResupplyCashReserve) / price;
                if (affordable <= 0)
                {
                    CloseShop();
                    _waitEstimated = priceFrom == BareValue;
                    WaitForMoney(me, cash, price * count - (cash - _ctx.Config.ResupplyCashReserve), count, price);
                    return;
                }
                if (affordable < count) _ctx.Log($"RESUPPLY: can only afford {affordable} of {count} at {price} each.");
                count = Math.Min(count, affordable);
            }

            _count = count; _added = 0; _cashBefore = cash;
            _tradeDone = _tradeDeclined = false; _doneAt = -1; _lastFeedback = null;
            _ctx.Log($"RESUPPLY: buying {count}x [{offer.Slot}] {offer.Item.Name} QL{offer.Item.Ql} ({(price > 0 ? $"{price} each, {priceFrom}" : "no price known")}), cash {cash}.");
            SetPhase(Phase.Adding);
        }

        private void AddingTick(LocalPlayer me)
        {
            if (_tradeDeclined) { _ctx.Log("RESUPPLY: the shop closed while adding."); Failed(me); return; }
            if (_added < _count)
            {
                if (_phaseTime < (_added + 1) * AddInterval) return;
                SendTrade(TradeAction.AddItem, 0x6F, _offer.Slot);
                _added++;
                return;
            }
            SendTrade(TradeAction.Accept, 0, 0);
            _ctx.Log($"RESUPPLY: added {_count}, accepting.");
            SetPhase(Phase.Settling);
        }

        private void SettlingTick(LocalPlayer me)
        {
            me.TryGetStat(Stat.Cash, out int cash);
            if (_tradeDone)
            {
                if (_doneAt < 0) _doneAt = _phaseTime;
                // The Cash stat can land a beat after the commit; wait for it (or a short while) to price the buy.
                if (cash == _cashBefore && _phaseTime - _doneAt < CashSettle) return;
                Bought(me, cash);
                return;
            }
            if (_tradeDeclined || _phaseTime > SettleTimeout)
            {
                // A commit we didn't see, but the money went: it went through.
                if (cash < _cashBefore) { Bought(me, cash); return; }
                _ctx.Log($"RESUPPLY: purchase not accepted ({(_tradeDeclined ? "declined" : "no answer")}){(_lastFeedback != null ? $" — '{_lastFeedback}'" : "")}.");
                Failed(me);
            }
        }

        private void Bought(LocalPlayer me, int cashNow)
        {
            int spent = _cashBefore - cashNow;
            _bought[_kind] += _count;
            _retryCount = 0;
            string priceNote = "";
            if (spent > 0)
            {
                double each = spent / (double)_count;
                priceNote = $" for {spent} credits ({each:0} each)";
                if (ItemValues.TryGet(_offer.Item.Id, _offer.Item.HighId, _offer.Item.Ql, out int value) && value > 0)
                {
                    int computed = Price(value, _machine, out string from, corrected: false);
                    EnsureLoaded();
                    _mem.Corrections[MachineKey(_machine)] = each / computed;
                    Save();
                    _ctx.Log($"RESUPPLY: '{_machineName}' paid {each:0} each, computed {computed} ({from}, Value {value}, CL {Cl()}) — correction x{each / computed:0.#####}.");
                }
            }
            _ctx.Log($"RESUPPLY: bought {_count}x {_offer.Item.Name} QL{_offer.Item.Ql}{priceNote}; cash {cashNow}. Now {Have(Supply.Stim)} stims, {Have(Supply.Recharger)} rechargers.");
            Tell($"Bought {_count}x {_offer.Item.Name} QL{_offer.Item.Ql}{priceNote}.");
            _offer = null;

            if (Needs().Count == 0) { Finish(me, $"Resupplied: {Have(Supply.Stim)} stims, {Have(Supply.Recharger)} rechargers, {cashNow} credits left."); return; }
            // Something else is still short — this machine may sell it too (it's first in line if it does).
            BuildCandidates(me, Needs());
            NextMachine(me);
        }

        // The server didn't take the buy. With the terminal's markup known the price was right and we checked
        // the money — so it's not money, give up on this machine. Priced at bare Value, it most likely is: try
        // again at half the count, down to one.
        private void Failed(LocalPlayer me)
        {
            CloseShop();
            bool priced = PriceEach(_offer.Item, out string from) > 0 && from != BareValue;
            me.TryGetStat(Stat.Cash, out int cash);
            if (!priced && _count > 1)
            {
                _retryCount = _count / 2;
                _ctx.Log($"RESUPPLY: retrying with {_retryCount}.");
                _candidates.Insert(0, _machine);
                NextMachine(me);
                return;
            }
            if (!priced)
            {
                _waitEstimated = false;
                WaitForMoney(me, cash, 0, 1, 0);
                return;
            }
            _ctx.Log($"RESUPPLY: '{_machineName}' refused the buy with the credits there — skipping it.");
            _retryCount = 0;
            NextMachine(me);
        }

        // Not enough credits. Tell the owner what we need, remind him every so often, and carry on by
        // ourselves the moment money arrives (his trade, a loot sale, anything that raises Cash).
        private void WaitForMoney(LocalPlayer me, int cash, int missing, int count, int price)
        {
            _cashAtWait = cash;
            _nagAccum = 0;
            _waitMissing = missing; _waitCount = count; _waitPrice = price;
            _move.Stop(me, _ctx.Config.SendIntervalMs);
            SetPhase(Phase.WaitMoney);
            Nag(cash);
        }

        private int _waitMissing, _waitCount, _waitPrice;
        private bool _waitEstimated;   // the price is Value-based, not one we've paid

        private void Nag(int cash)
        {
            string item = $"{_offer.Item.Name} QL{_offer.Item.Ql}";
            _ctx.Log($"RESUPPLY: short of credits for {item} (cash {cash}, price {_waitPrice}) — asking the owner.");
            Tell(_waitPrice > 0
                ? $"I need {(_waitEstimated ? "about " : "")}{_waitMissing} more credits to buy {_waitCount}x {item} ({(_waitEstimated ? "~" : "")}{_waitPrice} each, I have {cash}). Trade me some and I'll carry on — or send 'resupply stop'."
                : $"I can't afford a single {item} (I have {cash} credits{(_lastFeedback != null ? $"; shop said: {_lastFeedback}" : "")}). Trade me some credits and I'll carry on — or send 'resupply stop'.");
        }

        private void WaitMoneyTick(LocalPlayer me, double dt)
        {
            me.TryGetStat(Stat.Cash, out int cash);
            if (cash > _cashAtWait)
            {
                _ctx.Log($"RESUPPLY: cash {_cashAtWait} -> {cash}, trying again.");
                Tell($"Got it, {cash} credits now — buying.");
                _retryCount = 0;
                _candidates.Insert(0, _machine);
                NextMachine(me);
                return;
            }
            if (cash < _cashAtWait) _cashAtWait = cash;

            _nagAccum += dt;
            if (_nagAccum >= _ctx.Config.ResupplyNagSeconds)
            {
                _nagAccum = 0;
                Nag(cash);
            }
        }

        // ---- Machines -----------------------------------------------------------

        // Terminals in reach, best first: ones we've seen sell what we need, then ones whose name suggests
        // medical supplies, then the rest by distance. Ones we've seen sell neither are left out.
        private void BuildCandidates(LocalPlayer me, List<Supply> needs)
        {
            EnsureLoaded();
            Vector3 at = me.MovementComponent.Position;
            var ranked = new List<(Identity id, int rank, float dist)>();
            foreach (VendingMachine vm in DynelManager.VendingMachines)
            {
                float d = Vector3.Distance(at, vm.Transform.Position);
                if (d > _ctx.Config.ResupplySearchRadius) continue;
                int rank;
                if (_mem.Machines.TryGetValue(MachineKey(vm.Identity), out string sold))
                {
                    if (!needs.Any(n => sold.Split(',').Contains(n.ToString()))) continue;
                    rank = 0;
                }
                else rank = (needs.Contains(Supply.Container)
                             ? !string.IsNullOrEmpty(vm.Name) && vm.Name.IndexOf("Container", StringComparison.OrdinalIgnoreCase) >= 0
                             : NameSuggestsSupplies(vm.Name)) ? 1 : 2;
                ranked.Add((vm.Identity, rank, d));
            }
            _candidates.Clear();
            _candidates.AddRange(ranked.OrderBy(r => r.rank).ThenBy(r => r.dist).Select(r => r.id));
        }

        private bool NameSuggestsSupplies(string name)
            => !string.IsNullOrEmpty(name) && _ctx.Config.ResupplyMachineKeywords != null
               && _ctx.Config.ResupplyMachineKeywords.Any(k => name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

        private void NextMachine(LocalPlayer me)
        {
            _stock = null; _offer = null;
            if (_candidates.Count == 0 && _survey)
            {
                Finish(me, $"Vendor debug done: logged {_surveyLogged} of {_surveyTotal} terminal(s) — see VENDMACHINE / SHOPUPDATE in aobuddy.log.");
                return;
            }
            if (_candidates.Count == 0)
            {
                List<Supply> left = Needs();
                Finish(me, left.Count == 0 ? "Resupplied."
                    : $"Couldn't find a terminal here selling {string.Join(" or ", left.Select(Plural))} I can use. Have {Have(Supply.Stim)} stims, {Have(Supply.Recharger)} rechargers.");
                return;
            }
            if (++_opens > (_survey ? int.MaxValue : MaxOpens)) { Finish(me, $"Gave up after {MaxOpens} terminal visits."); return; }

            _machine = _candidates[0];
            _candidates.RemoveAt(0);
            _machineName = DynelManager.Find(_machine, out VendingMachine vm) && !string.IsNullOrEmpty(vm.Name) ? vm.Name : _machine.ToString();
            _bestDist = float.MaxValue;
            _lastProgressAt = 0;
            SetPhase(Phase.Approach);
        }

        private void Open(LocalPlayer me)
        {
            _stock = null; _lastFeedback = null;
            _tradeDone = _tradeDeclined = false;
            GameCommands.UseObject(me, _machine);
            _ctx.Log($"RESUPPLY: opening '{_machineName}' ({_machine}).");
            SetPhase(Phase.Opening);
        }

        private void CloseShop() => SendTrade(TradeAction.Decline, 0, 0);

        private void SendTrade(TradeAction action, int p3, int p4)
        {
            Client.Send(new TradeMessage
            {
                Version = 2,
                Action = action,
                Param1 = (int)_machine.Type,
                Param2 = _machine.Instance,
                Param3 = p3,
                Param4 = p4,
            });
        }

        private void Finish(LocalPlayer me, string text)
        {
            _ctx.Log("RESUPPLY: " + text);
            Tell(text);
            if (me != null) _move.Stop(me, _ctx.Config.SendIntervalMs);
            Reset();
        }

        private void Reset()
        {
            _phase = Phase.Idle; _phaseTime = 0;
            _survey = false;
            _candidates.Clear();
            _machine = Identity.None; _machineName = "";
            _stock = null; _offer = null;
            _retryCount = 0;
        }

        private void SetPhase(Phase p) { _phase = p; _phaseTime = 0; }

        // ---- Supplies -----------------------------------------------------------

        // By EXACT name: a keyword like "Stim" also matches Boosted Stim, Burst of Speed Stim, Swim Stim... —
        // the pharmacy sells all of them, and the first run bought 25 Boosted Stims as "stims".
        private bool Is(Item it, Supply s)
            => it?.Name != null && string.Equals(it.Name, Name(s), StringComparison.OrdinalIgnoreCase);

        private string Name(Supply s) => s == Supply.Stim ? _ctx.Config.ResupplyStimName
                                       : s == Supply.Container ? _ctx.Config.ResupplyContainerName : _ctx.Config.ResupplyRechargerName;

        // How many we carry that we can use: every stack of the item (main inventory and open bags) whose
        // First Aid / Treatment requirement we meet, by stack count.
        private int Have(Supply s)
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            List<Item> items = SupportController.AllInvItems();
            if (s == Supply.Container) return items.Count(it => Is(it, s));
            return items.Where(it => Is(it, s) && SupportController.MeetsHealReqs(it, me)).Sum(it => Math.Max(1, it.Count));
        }

        private int Want(Supply s) => s == Supply.Stim ? _ctx.Config.ResupplyStimTarget
                                    : s == Supply.Container ? _containersWanted : _ctx.Config.ResupplyRechargerTarget;

        // Still to buy this run: what the inventory says is missing, but never more than the run set out to
        // buy — so a purchase the inventory model misses can't make us buy the same stims over and over.
        private int Remaining(Supply s)
        {
            if (s == Supply.Container) return Math.Max(0, _containersWanted - (_bought.TryGetValue(s, out int b) ? b : 0));
            int byInventory = Want(s) - Have(s);
            if (!_haveAtStart.TryGetValue(s, out int start)) return Math.Max(0, byInventory);
            return Math.Max(0, Math.Min(byInventory, Want(s) - start - _bought[s]));
        }

        private List<Supply> Needs() => (_containersWanted > 0 ? new[] { Supply.Container } : new[] { Supply.Stim, Supply.Recharger })
                                        .Where(s => Remaining(s) > 0).ToList();

        private static string Plural(Supply s) => s == Supply.Stim ? "stims" : s == Supply.Container ? "bags" : "rechargers";

        // ---- Memory ---------------------------------------------------------------

        private static string MachineKey(Identity id) => $"{(int)Playfield.ModelId}:{id.Instance}";

        private const string BareValue = "bare Value, terminal markup unknown";

        // What one of these costs at the current terminal. 0 = no Value for it.
        private int PriceEach(Item it, out string from)
        {
            from = "";
            if (!ItemValues.TryGet(it.Id, it.HighId, it.Ql, out int value) || value <= 0) return 0;
            return Price(value, _machine, out from);
        }

        // Value x the terminal template's SellModifier x our CL discount (ItemValues.ShopPrice), times the
        // correction learned from an earlier purchase at this terminal. `from` says what went into it.
        private int Price(int value, Identity machine, out string from, bool corrected = true)
        {
            int price;
            if (DynelManager.Find(machine, out VendingMachine vm) && vm.Stats != null
                && vm.Stats.TryGetValue(Stat.StaticInstance, out int tmpl) && ItemValues.TryGetShopModifiers(tmpl, out int sell, out _) && sell > 0)
            {
                price = ItemValues.ShopPrice(value, sell, Cl());
                from = $"SellModifier {sell}, CL discount {ItemValues.ClDiscount(Cl()):0.##}";
            }
            else { price = value; from = BareValue; }

            EnsureLoaded();
            if (corrected && _mem.Corrections.TryGetValue(MachineKey(machine), out double c) && c > 0 && Math.Abs(c - 1.0) > 0.001)
            {
                price = (int)Math.Ceiling(price * c);
                from += $", corrected x{c:0.####}";
            }
            return price;
        }

        private static int Cl()
            => DynelManager.LocalPlayer != null && DynelManager.LocalPlayer.TryGetStat(Stat.ComputerLiteracy, out int cl) ? cl : 0;

        private void Remember(Identity machine, List<Supply> sells)
        {
            EnsureLoaded();
            _mem.Machines[MachineKey(machine)] = string.Join(",", sells);
            Save();
        }

        private void EnsureLoaded()
        {
            if (_mem != null) return;
            _mem = JsonStore.Load<Memory>(_file, _ctx.Log) ?? new Memory();
        }

        private void Save()
        {
            JsonStore.Save(_file, JsonConvert.SerializeObject(_mem, Formatting.Indented), _ctx.Log);
        }

        private void Tell(string text)
        {
            _ctx.TellOwner(text);
        }
    }
}