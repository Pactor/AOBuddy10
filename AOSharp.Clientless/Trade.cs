using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AOSharp.Clientless
{
    public class KnuBotTradeWindowCache
    {
        public List<Item> Items = new List<Item>();
        public int Credits;
    }

    public static class Trade
    {
        public const int TRADE_CAPACITY = 30;
        public const int TRADE_STARTSLOT = 0x0;
        public const int TRADE_ENDSLOT = TRADE_STARTSLOT + TRADE_CAPACITY;

        public static Identity CurrentTarget
        {
            get { return _tradeTarget; }
            internal set { _tradeTarget = value; }
        }

        public static bool IsTrading => _tradeTarget != Identity.None;

        public static TradeStatus Status;

        public static Action<Identity> TradeOpened;
        public static Action<Identity, TradeStatus> TradeStatusChanged;

        public static KnuBotTradeWindowCache PlayerWindowCache => _windowCache[TradeWindow.Player];

        public static KnuBotTradeWindowCache TargetWindowCache => _windowCache[TradeWindow.Target];

        private static Dictionary<TradeWindow, KnuBotTradeWindowCache> _windowCache = new Dictionary<TradeWindow, KnuBotTradeWindowCache>
        {
            { TradeWindow.Player, new KnuBotTradeWindowCache()},
            { TradeWindow.Target, new KnuBotTradeWindowCache()},
        };

        private static Identity _tradeTarget = Identity.None;

        internal static int? GetNextAvailableSlot(TradeWindow window) => Enumerable.Range(TRADE_STARTSLOT, TRADE_ENDSLOT).Except(_windowCache[window].Items.Select(x => x.Slot.Instance)).FirstOrDefault();

        public static void Open(Identity target) => TradeMessage(TradeAction.Open, target);

        public static void Decline() => TradeMessage(TradeAction.Decline, _tradeTarget);

        public static void AddItem(int itemIndex)
        {
            Client.Send(new TradeMessage
            {
                Unknown1 = 2,
                Action = TradeAction.AddItem,
                Param1 = (int)_tradeTarget.Type,
                Param2 = _tradeTarget.Instance,
                Param3 = 0x6f,
                Param4 = itemIndex
            });
        }

        public static void AddItem(Identity slot)
        {
            Client.Send(new TradeMessage
            {
                Unknown1 = 2,
                Action = TradeAction.AddItem,
                Param1 = (int)DynelManager.LocalPlayer.Identity.Type,
                Param2 = DynelManager.LocalPlayer.Identity.Instance,
                Param3 = (int)slot.Type,
                Param4 = slot.Instance,
            });
        }

        public static void Accept()
        {
            Client.Send(new TradeMessage
            {
                Unknown1 = 2,
                Action = TradeAction.Accept,
                Param1 = (int)_tradeTarget.Type,
                Param2 = _tradeTarget.Instance,
            });
        }

        public static void Confirm()
        {
            Client.Send(new TradeMessage
            {
                Unknown1 = 2,
                Action = TradeAction.Confirm,
                Param1 = (int)_tradeTarget.Type,
                Param2 = _tradeTarget.Instance,
            });
        }

        private static void Reset()
        {
            _tradeTarget = Identity.None;
            Status = TradeStatus.None;

            _windowCache[TradeWindow.Player].Credits = 0;
            _windowCache[TradeWindow.Target].Credits = 0;
            _windowCache[TradeWindow.Player].Items.Clear();
            _windowCache[TradeWindow.Target].Items.Clear();
        }

        internal static void OnTemplateAction(int lowId, int highId, int ql)
        {
            if (NoAvailableSlots(TradeWindow.Target))
                return;

            Item item = new Item(Identity.None, Identity.None, lowId, highId, ql);
            RegisterItem(TradeWindow.Target, item);
            //ShowDebug();
        }

        internal static void OnTradeMessageSent(TradeMessage tradeMsg)
        {
            switch (tradeMsg.Action)
            {
                case TradeAction.Accept:
                    if (Status != TradeStatus.Confirm)
                        Status = TradeStatus.Accept;
                    break;
            }
        }

        internal static void OnTradeMessageReceived(TradeMessage tradeMsg)
        {
            if (tradeMsg.Identity != DynelManager.LocalPlayer.Identity && tradeMsg.Action != TradeAction.UpdateCredits)
                return;


            var iden = new Identity((IdentityType)tradeMsg.Param1, tradeMsg.Param2);

            switch (tradeMsg.Action)
            {
                case TradeAction.Open:
                    _tradeTarget = new Identity((IdentityType)tradeMsg.Param1, tradeMsg.Param2);
                    TradeOpened?.Invoke(_tradeTarget);
                    TradeStatusChanged?.Invoke(_tradeTarget, TradeStatus.Opened);
                    break;
                case TradeAction.Decline:
                    DeclineAction();
                    TradeStatusChanged?.Invoke(iden, TradeStatus.Declined);
                    break;
                case TradeAction.Complete:
                    CompleteAction();
                    TradeStatusChanged?.Invoke(iden, TradeStatus.Finished);
                    break;
                case TradeAction.AddItem:
                    PlayerAddItemAction(new Identity((IdentityType)tradeMsg.Param3, tradeMsg.Param4));
                    break;
                case TradeAction.RemoveItem:
                    RemoveItemAction(tradeMsg.Param2, tradeMsg.Param4);
                    break;
                case TradeAction.Confirm:
                    Status = TradeStatus.Confirm;
                    TradeStatusChanged?.Invoke(iden, Status);
                    break;
                case TradeAction.Accept:
                    Status = TradeStatus.Accept;
                    TradeStatusChanged?.Invoke(iden, Status);
                    break;
                case TradeAction.UpdateCredits:
                    TradeWindow wind = tradeMsg.Identity == DynelManager.LocalPlayer.Identity ? TradeWindow.Player : TradeWindow.Target;
                    _windowCache[wind].Credits = tradeMsg.Param2;
                    break;
                case TradeAction.OtherPlayerAddItem:
                    if (FullUpdateProxy.Find(new Identity((IdentityType)tradeMsg.Param3, tradeMsg.Param4), out SimpleItem simpleItem))
                        OtherPlayerAddItemAction(TradeWindow.Target, simpleItem); // id data from chestfullupdate, weaponfullupdate, simpleitemfullupdate
                    break;
            }

            //ShowDebug();
        }

        private static void ShowDebug()
        {
            Logger.Debug($"PLAYER KNUBOT TRADE WINDOW:");

            foreach (var item in _windowCache[TradeWindow.Player].Items)
                Logger.Debug($"{item.Id}");

            Logger.Debug($"Credits: {_windowCache[TradeWindow.Player].Credits}");
            Logger.Debug($"Items Count: {_windowCache[TradeWindow.Player].Items.Count()}");

            Logger.Debug($"TARGET KNUBOT TRADE WINDOW:");

            foreach (var item in _windowCache[TradeWindow.Target].Items)
                Logger.Debug($"{item.Id}");

            Logger.Debug($"Credits: {_windowCache[TradeWindow.Target].Credits}");
            Logger.Debug($"Items Count: {_windowCache[TradeWindow.Target].Items.Count()}");
        }

        private static bool FindItem(TradeWindow window, int slot, out Item item)
        {
            return (item = _windowCache[window].Items.FirstOrDefault(x => x.Slot.Instance == slot)) != null;
        }

        private static void OtherPlayerAddItemAction(TradeWindow window, SimpleItem simpleItem)
        {
            var nextSlot = GetNextAvailableSlot(window);

            if (nextSlot == null)
                return;

            Item item = new Item(Identity.None, simpleItem.Identity, simpleItem.ACGItem);
            RegisterItem(window, item);
        }

        private static void DeclineAction()
        {
            var nextAvailableSlot = Inventory.GetNextAvailableSlot();

            if (Inventory.NoAvailableSlots) //TODO: Overflow
                return;

            foreach (var item in _windowCache[TradeWindow.Player].Items.OrderBy(x => x.Slot.Instance).ToList())
                Inventory.AddToNextAvailableSlot(item, false);

            Reset();
        }

        private static void CompleteAction()
        {
            foreach (var item in _windowCache[TradeWindow.Target].Items.OrderBy(x => x.Slot.Instance).ToList())
            {
                //Logger.Information($"Received item {item.Id} in trade.");
                Inventory.AddToNextAvailableSlot(item);
            }

            foreach (var item in _windowCache[TradeWindow.Player].Items)
            {
                //Logger.Information($"Gave item {item.Id} in trade.");
                Inventory.RemoveItem(item); 
            }

            Reset();
        }

        private static void PlayerAddItemAction(Identity itemSlot)
        {
            if (!Inventory.Find(itemSlot, out Item item))
                return;

            if (NoAvailableSlots(TradeWindow.Player))
                return;

            Inventory.RemoveItem(item, false);

            RegisterItem(TradeWindow.Player, item);
        }

        private static void RemoveItemAction(int dynelInstance, int itemSlot)
        {
            TradeWindow window = dynelInstance == DynelManager.LocalPlayer.Identity.Instance ? TradeWindow.Player : TradeWindow.Target;

            if (!FindItem(window, itemSlot, out Item item))
                return;

            if (Inventory.NoAvailableSlots)
                return;

            RemoveItem(window, item);
            Inventory.AddToNextAvailableSlot(item);
        }

        private static void TradeMessage(TradeAction tradeAction, Identity target)
        {
            Client.Send(new TradeMessage
            {
                Unknown1 = 2,
                Action = tradeAction,
                Param1 = (int)target.Type,
                Param2 = target.Instance,
            });
        }

        private static void RegisterItem(TradeWindow window, Item item)
        {
            //Logger.Information($"Added item {item.Id} to {window}");
            item.Slot = new Identity(IdentityType.KnuBotTradeWindow, (int)GetNextAvailableSlot(window));
            _windowCache[window].Items.Add(item);
        }

        private static bool NoAvailableSlots(TradeWindow window) => GetNextAvailableSlot(window) == null;

        private static void RemoveItem(TradeWindow window, Item item) => _windowCache[window].Items.Remove(item);
    }
}

public enum TradeWindow
{
    Player,
    Target
}

public enum TradeStatus
{
    None,
    Accept,
    Confirm,
    Finished,
    Declined,
    Opened
}