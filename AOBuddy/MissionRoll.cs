using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOBuddy
{
    /// <summary>
    /// MISSION ROLL — rolling missions at a SOLO mission terminal and accepting one. Every message here is copied
    /// from the owner's own client (capture 20260923-201746, solo terminal 56001:-1073741024 in Borealis):
    ///   - the terminal is Used once (GenericCmd Use);
    ///   - each roll is a QuestAlternative carrying the difficulty and all six sliders (sent with EVERY roll),
    ///     Scope Solo, the terminal identity and no missions; the bot's serializer reproduces the owner's bytes;
    ///   - the server answers with a QuestAlternative holding 5 missions, each with its type (MissionIcon: 0x2C47
    ///     find person, 0x2C49 find item, 0x2C4E repair, 0x2C41 return item, 0x2C42 kill person), destination
    ///     playfield and door position, credits, XP and reward item;
    ///   - accepting is CreateQuest(mission identity); the server puts the mission key in the bags and sends
    ///     the QuestFullUpdate.
    /// Nothing here moves the bot: `mission roll` works at the terminal it is standing next to.
    /// </summary>
    public class MissionRoll
    {
        private readonly BotContext _ctx;

        public const int TypeReturnItem = 0x2C41, TypeKillPerson = 0x2C42, TypeFindPerson = 0x2C47, TypeFindItem = 0x2C49, TypeRepair = 0x2C4E;

        /// <summary>The last list the server sent, in its order.</summary>
        public IReadOnlyList<MissionInfo> Offered => _offered;
        private List<MissionInfo> _offered = new List<MissionInfo>();
        private Identity? _terminal;
        private MissionScope _scope = MissionScope.Solo;   // Team at a team terminal (capture 20260923-120056), Solo at a solo one
        private readonly HashSet<Identity> _usedTerminals = new HashSet<Identity>();
        private double _rollAt = -1;           // clock time to send the pending roll (after the terminal Use)
        private double _clock;
        private Action<string> _reply;
        public event Action<IReadOnlyList<MissionInfo>> ListArrived;
        public event Action<MissionInfo> Accepted;

        public MissionRoll(BotContext ctx) { _ctx = ctx; }

        public static string TypeName(int code)
        {
            switch (code)
            {
                case TypeFindPerson: return "find person";
                case TypeFindItem: return "find item";
                case TypeRepair: return "repair";
                case TypeReturnItem: return "return item";
                case TypeKillPerson: return "kill person";
                case 0: return "mission";
                default: return $"type 0x{code:X}";
            }
        }

        /// <summary>The types blitz can finish (MissionController): find person, find item, repair.</summary>
        public static bool BlitzCan(int code) => code == TypeFindPerson || code == TypeFindItem || code == TypeRepair;

        // ---- Commands ----------------------------------------------------------------------------

        /// <summary>'mission roll' / 'mission accept n' / 'mission list'. Returns false if it isn't ours.</summary>
        public bool Command(string sub, string arg, Action<string> reply)
        {
            switch (sub)
            {
                case "roll": Roll(reply); return true;
                case "list": reply(_offered.Count == 0 ? "No mission list yet - 'mission roll' at a terminal." : Describe()); return true;
                case "accept":
                    if (!int.TryParse(arg, out int n) || n < 1 || n > _offered.Count) { reply($"Usage: mission accept <1-{Math.Max(_offered.Count, 5)}> (from the last roll)"); return true; }
                    Accept(_offered[n - 1], reply);
                    return true;
            }
            return false;
        }

        /// <summary>A team terminal: its name says so ('Basic Team Mission Terminal', log 2026-09-24 08:36:16).
        /// Nothing in the game data lists terminals, so the name is all there is to go by.</summary>
        public static bool IsTeamTerminal(Dynel d) => d?.Name != null && d.Name.IndexOf("Team", StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>In a team: team missions from a team terminal. Not: solo missions from a solo terminal.</summary>
        public static bool WantTeam(LocalPlayer me) => me != null && me.GetStat(Stat.Team) != 0;

        /// <summary>The nearest terminal within the given metres of a point whose kind (team/solo) is the one
        /// the bot's team status calls for.</summary>
        public static Dynel MatchingTerminal(LocalPlayer me, Vector3 near, float within)
        {
            bool team = WantTeam(me);
            return DynelManager.AllDynels
                .Where(d => d != null && d.Identity.Type == IdentityType.MissionTerminal && IsTeamTerminal(d) == team
                            && Vector3.Distance(d.Transform.Position, near) <= within)
                .OrderBy(d => Vector3.Distance(d.Transform.Position, near)).FirstOrDefault();
        }

        /// <summary>Roll at the nearest terminal within 6 m of the kind his team status calls for.</summary>
        public void Roll(Action<string> reply)
        {
            var me = DynelManager.LocalPlayer;
            if (me == null) { reply("Not in game."); return; }
            bool team = WantTeam(me);
            string kind = team ? "team" : "solo";
            var term = MatchingTerminal(me, me.Transform.Position, 6f);
            if (term == null)
            {
                var any = DynelManager.AllDynels.Where(d => d != null && d.Identity.Type == IdentityType.MissionTerminal)
                                                .OrderBy(d => me.DistanceFrom(d)).FirstOrDefault();
                reply(any == null ? "No mission terminal in this playfield."
                    : $"I'm {(team ? "" : "not ")}in a team, so I need a {kind} mission terminal within 6 m; the nearest terminal is '{any.Name}' {me.DistanceFrom(any):0} m away.");
                return;
            }
            _terminal = term.Identity; _scope = team ? MissionScope.Team : MissionScope.Solo; _reply = reply;
            if (_usedTerminals.Add(term.Identity))
            {
                // The owner's client Uses the terminal once, then rolls; rolls after that need no Use.
                GameCommands.UseObject(me, term.Identity);
                _ctx.Log($"MISSIONROLL: used terminal {term.Identity} ('{term.Name}').");
                _rollAt = _clock + 1.0;
            }
            else SendRoll();
        }

        /// <summary>Set by the want run to aim the mission QL (step 4); null = the configured difficulty.</summary>
        public int? DifficultyOverride;
        /// <summary>The difficulty the last roll was sent with.</summary>
        public int LastDifficulty { get; private set; }

        private void SendRoll()
        {
            _rollAt = -1;
            var me = DynelManager.LocalPlayer;
            if (me == null || !_terminal.HasValue) return;
            var c = _ctx.Config;
            var sliders = new MissionSliders
            {
                Difficulty = (byte)Math.Max(0, Math.Min(255, DifficultyOverride ?? (string.Equals(c.MissionStyle, "fight", StringComparison.OrdinalIgnoreCase)
                    ? Math.Max(c.MissionDifficulty, c.MissionFightDifficulty) : c.MissionDifficulty))),
                GoodBad = Slider(c.MissionSliderGoodBad),
                OrderChaos = Slider(c.MissionSliderOrderChaos),
                OpenHidden = Slider(c.MissionSliderOpenHidden),
                PhysicalMystical = Slider(c.MissionSliderPhysicalMystical),
                HeadonStealth = Slider(c.MissionSliderHeadonStealth),
                CreditsXp = Slider(c.MissionSliderCreditsXp),
            };
            LastDifficulty = sliders.Difficulty;
            Client.Send(new QuestAlternativeMessage
            {
                VersionId = 4, MissionSliders = sliders, Unknown2 = 0, Scope = _scope,
                Terminal = _terminal.Value, MissionDetails = new MissionInfo[0],
            });
            _ctx.Log($"MISSIONROLL: roll sent: difficulty {sliders.Difficulty}, sliders {sliders.GoodBad},{sliders.OrderChaos},{sliders.OpenHidden},{sliders.PhysicalMystical},{sliders.HeadonStealth},{sliders.CreditsXp} (bytes), scope {_scope}, at {_terminal.Value}.");
        }

        // Slider value (-100..+100, 0 = middle) to its signed byte on the wire: -100 = 0x9C, +100 = 0x64.
        private static byte Slider(int value) => unchecked((byte)(sbyte)Math.Max(-100, Math.Min(100, value)));

        public void Accept(MissionInfo m, Action<string> reply)
        {
            Client.Send(new CreateQuestMessage { MissionId = m.MissionIdentity });
            _ctx.Log($"MISSIONROLL: accepted {m.MissionIdentity} ({TypeName(m.MissionIcon)} in {Zone(m)}).");
            reply?.Invoke($"Accepted: {Line(m)}");
            Accepted?.Invoke(m);
        }

        // ---- Wire --------------------------------------------------------------------------------

        public void Tick(double dt)
        {
            _clock += dt;
            if (_rollAt >= 0 && _clock >= _rollAt) SendRoll();
        }

        public void OnMessage(Message m)
        {
            if (!(m?.Body is QuestAlternativeMessage q) || q.MissionDetails == null || q.MissionDetails.Length == 0) return;
            _offered = q.MissionDetails.ToList();
            _ctx.Log($"MISSIONROLL: {_offered.Count} missions offered (seed {q.Unknown2}):");
            for (int i = 0; i < _offered.Count; i++) _ctx.Log($"MISSIONROLL:   {i + 1}) {Line(_offered[i])}  [{_offered[i].MissionIdentity}]");
            _reply?.Invoke(Describe());
            ListArrived?.Invoke(_offered);
        }

        // ---- Text --------------------------------------------------------------------------------

        public string Describe()
        {
            var parts = _offered.Select((m, i) => $"{i + 1}) {Line(m)}{(Allowed(m, out string why) ? "" : " [skip: " + why + "]")}");
            return string.Join(" | ", parts);
        }

        public static string Zone(MissionInfo m) =>
            Playfield.TryGetPlayfieldNameFromId(m.Playfield.Instance, out string n) ? n : $"pf {m.Playfield.Instance}";

        public static string Line(MissionInfo m)
        {
            string item = "";
            var r = m.MissionItemData?.FirstOrDefault();
            if (r != null && ItemData.Find(r.LowId, out DummyItem it) && it != null) item = $", {it.Name} QL{r.Ql}";
            return $"{TypeName(m.MissionIcon)} in {Zone(m)} ({m.Location.X:0},{m.Location.Z:0}), {m.Credits:N0} cr{item}";
        }

        /// <summary>Whether a mission fits the config: a type blitz can finish, in an allowed zone.</summary>
        public bool Allowed(MissionInfo m, out string why)
        {
            if (!BlitzCan(m.MissionIcon)) { why = TypeName(m.MissionIcon) + " not supported"; return false; }
            var zones = _ctx.Config.MissionZones;
            if (zones != null && zones.Count > 0)
            {
                string name = Zone(m);
                bool ok = zones.Any(z => string.Equals(z?.Trim(), name, StringComparison.OrdinalIgnoreCase)
                                         || (int.TryParse(z, out int id) && id == m.Playfield.Instance));
                if (!ok) { why = $"{name} is not in MissionZones"; return false; }
            }
            why = null;
            return true;
        }
    }
}
