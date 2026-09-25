using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOSharp.Clientless
{
    public static class Inventory
    {
        public const int INVENTORY_CAPACITY = 30;
        public const int INVENTORY_START = 0x40;
        public const int INVENTORY_END = INVENTORY_START + INVENTORY_CAPACITY;
        public static int NumFreeSlots => 30 - _items.Where(x => x.Slot.Type == IdentityType.Inventory).Count();
        public static bool NoAvailableSlots => GetNextAvailableSlot() == null;

        public static Bank Bank = new Bank();

        public static IReadOnlyList<Container> Containers => _containers;
        public static IReadOnlyList<Item> Items => _items;
        public static IReadOnlyList<UniqueItem> UniqueItems => _items.OfType<UniqueItem>().ToList();
        public static IReadOnlyList<TempItem> TempItems => _items.OfType<TempItem>().ToList();
        private static Item _lastProxyItem;
        private static List<Item> _items = new List<Item>();
        private static List<Container> _containers = new List<Container>();

        public static Action<Container> ContainerOpened;
        public static Action<Item> ItemAdded;
        public static Action<Item> ItemRemoved;
        public static Action<Item> BankItemAdded;
        public static Action<Item> BankItemRemoved;
        public static Action<Container, Item> ContainerItemAdded;
        public static Action<Container, Item> ContainerItemRemoved;
        public static Action BankOpen;

        internal static int? GetNextAvailableSlot(int invStart, int invEnd, IEnumerable<Item> items) => Enumerable.Range(invStart, invEnd).Except(items.Select(x => (x.Slot.Instance & 0xFF))).FirstOrDefault();
  
        public static int? GetNextAvailableSlot() => GetNextAvailableSlot(INVENTORY_START, INVENTORY_CAPACITY, _items);

        public static bool Find(Identity slot, out Item item) => (item = Items?.FirstOrDefault(x => x.Slot == slot)) != null;

        public static bool Find(string name, out Item item) => (item = Items?.FirstOrDefault(x => x.Name == name)) != null;

        public static bool Find(int id, out Item item) => (item = Items?.FirstOrDefault(x => x.Id == id || x.HighId == id)) != null;

        public static bool Find(int lowId, int highId, out Item item) => (item = Items?.FirstOrDefault(x => x.Id == lowId && x.HighId == highId)) != null;

        public static List<Item> FindAll(int id) => Items?.Where(x => x.Id == id || x.HighId == id).ToList();

        public static List<Item> FindAll(IEnumerable<int> ids) => Items?.Where(x => ids.Contains(x.Id)).ToList();

        public static List<Item> FindAll(int lowId, int highId) => Items?.Where(x => x.Id == lowId && x.HighId == highId).ToList();

        public static List<Item> FindAll(string name) => Items?.Where(x => x.Name == name).ToList();

        internal static void OnFullCharacterMessage(InventorySlot[] inventorySlots)
        {
            // NULL when this FullCharacter came from the corrected fallback reader: FullCharacterReader
            // SKIPS the InventorySlots section (SkipX3F1) and never assigns it. Walking that null threw a
            // NullReferenceException out of the FullCharacter callback, which NetworkSession then reported
            // as "Dropping unparseable packet" — the packet parsed fine, the HANDLER died.
            // Worse, the two lines below had ALREADY WIPED the inventory before the throw, leaving the bot
            // with no items at all. Keep what we have when there is nothing to replace it with.
            if (inventorySlots == null)
                return;

            _items = new List<Item>();
            _containers = new List<Container>();
            RegisterItems(_items, inventorySlots);
        }

        internal static void OnContainerUpdate(Identity invIdentity, InventorySlot[] inventorySlots, int handle)
        {
            if (_containers.Find(invIdentity, out Container container))
                _containers.Remove(container);

            container = new Container(invIdentity, handle);
            _containers.Add(container);

            container.RegisterItems(inventorySlots);

            ContainerOpened?.Invoke(container);
        }

        // A bag bought from a shop reaches us only as InventoryUpdate (the bag's container, Handle = a container
        // number, not a slot) plus ChestItemFullUpdate (0x465A5D73), which the stock serializer can't read and
        // NetworkSession hands on raw - so the new bag never showed up among the items and MissionRun thought the
        // purchase had failed (21:55, 2026-09-24). Capture 20260923-234203 s8 seq 106-107: bag 51017:27906825,
        // version 11, owner = the player, InventoryId 101, BodyLocation 0x6F (next free slot), stats 3F1-counted
        // with the template in 702/703 (ACGItemTemplateID/ID2). Add it where the client would put it. Chests out
        // in the world (mission containers) have another owner and are left alone.
        internal static void OnChestItemRaw(byte[] b)
        {
            if (b == null || b.Length < 59 || b[32] != 0x0B) return;
            int R(int p) => (b[p] << 24) | (b[p + 1] << 16) | (b[p + 2] << 8) | b[p + 3];
            var id = new Identity((IdentityType)R(20), R(24));
            var me = DynelManager.LocalPlayer;
            if (id.Type != IdentityType.Container || me == null) return;
            if (new Identity((IdentityType)R(33), R(37)) != me.Identity) return;
            if (_items.Any(i => i.UniqueIdentity == id) || Bank.Items.Any(i => i.UniqueIdentity == id)) return;
            int low = 0, high = 0, ql = 1, n = R(55) / 1009 - 1;
            for (int k = 0, p = 59; k < n && p + 8 <= b.Length; k++, p += 8)
            {
                int key = R(p), val = R(p + 4);
                if (key == 702) low = val; else if (key == 703) high = val; else if (key == 54) ql = val;
            }
            int body = b[54];
            int? slot = body == 0x6F ? GetNextAvailableSlot()
                      : body >= INVENTORY_START && body < INVENTORY_START + INVENTORY_CAPACITY && !_items.Any(i => i.Slot.Type == IdentityType.Inventory && i.Slot.Instance == body) ? body : (int?)null;
            if (slot == null) return;
            var item = new Item(new Identity(IdentityType.Inventory, slot.Value), id, low, high == 0 ? low : high, ql);
            _items.Add(item);
            ItemAdded?.Invoke(item);
        }

        internal static void ResetContainers()
        {
            foreach (Container container in _containers)
            {
                container.Items = new List<Item>();
                container.Handle = 0;
            }

            Bank.IsOpen = false;
        }

        // Count is the stack size the server created (a shop purchase of 7 stims arrives as one AddTemplate
        // with Count 7); without it the stack read as a single item.
        internal static void OnAddTemplateMessage(int lowId, int highId, int ql, int count = 1)
        {
            var item = new Item(Identity.None, Identity.None, lowId, highId, ql) { Count = Math.Max(1, count) };
            AddToNextAvailableSlot(item);
            RegisterLastItem(item);
        }

        internal static void OnTemplateMessage(int lowId, int highId, int ql)
        {
            RegisterLastItem(lowId, highId, ql);
        }

        internal static void OnBankUpdate(BankMessage bankMsg)
        {
            Bank.RegisterItems(bankMsg.BankSlots);
            Bank.IsOpen = true;
            BankOpen?.Invoke();
        }

        internal static void OnContainerAddItem(Identity source, Identity target, int slot)
        {
            // We will map these as needed
            if (source.Type == IdentityType.OverflowWindow && source.Instance == 0 &&
                target.Type == IdentityType.OverflowWindow && target.Instance == DynelManager.LocalPlayer.Identity.Instance)
            {
                OnSpawnItemAction();
            }
            else
            {
                OnMoveItemAction(source, target, slot);
            }
        }

        private static void OnMoveItemAction(Identity source, Identity target, int slot)
        {
            switch (source.Type)
            {
                case IdentityType.Inventory:
                case IdentityType.ArmorPage:
                case IdentityType.WeaponPage:
                case IdentityType.ImplantPage:
                case IdentityType.SocialPage:
                    if (Find(source, out Item invItem))
                    {
                        if (target.Type == IdentityType.Bank)
                            RemoveItem(invItem);
                        else
                            _items.Remove(invItem);

                        ItemRemoved?.Invoke(invItem);
                        OnContainerAction(target, invItem, slot);
                    }
                    break;
                case IdentityType.Backpack:
                    if (_containers.Find(source, out Item bagItem))
                    {
                        _containers.RemoveItem(bagItem, out Container container);
                        ContainerItemRemoved?.Invoke(container, bagItem);
                        OnContainerAction(target, bagItem, slot);
                    }
                    break;
                case IdentityType.BankByRef:
                    if (Bank.Items.Find(source, out Item bankItem))
                    {
                        Bank.RemoveItem(bankItem);
                        BankItemRemoved?.Invoke(bankItem);
                        OnContainerAction(target, bankItem, slot);
                    }
                    break;
                default:
                    Logger.Information($"OnMoveItemAction IdentityType {source.Type} not mapped. This shouldn't happen.");
                    break;
            }
        }

        private static void OnContainerAction(Identity target, Item sourceItem, int slot)
        {
            if (target.Type == IdentityType.Container)
            {
                if (_containers.Find(target, out Container container))
                {
                    container.AddItem(sourceItem);

                    ContainerItemAdded?.Invoke(container, sourceItem);
                }
            }
            else if (target.Type == IdentityType.Bank)
            {
                Bank.AddItemToAvailableSlot(sourceItem);
                BankItemAdded?.Invoke(sourceItem);
            }
            else if (target == DynelManager.LocalPlayer.Identity)
            {
                sourceItem.Slot = new Identity(GetSlotType(slot), slot == 0x6F ? GetNextAvailableSlot().Value : slot);
                _items.Add(sourceItem);
                ItemAdded?.Invoke(sourceItem);
            }
        }

        private static void OnSpawnItemAction()
        {
            if (_lastProxyItem == null) return;
            AddToNextAvailableSlot(_lastProxyItem);
            ItemAdded?.Invoke(_lastProxyItem);
        }

        private static void RegisterItems(List<Item> items, InventorySlot[] inventorySlots)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                var invSlot = inventorySlots[i];
                Item item = new Item(new Identity(GetSlotType(invSlot.Placement), invSlot.Placement), invSlot.Identity, invSlot.ItemLowId, invSlot.ItemHighId, invSlot.Quality);
                item.Count = invSlot.Count;   // stack size (for accurate supply counts)

                if (invSlot.Identity.Type == IdentityType.Container)
                    OnContainerUpdate(invSlot.Identity, new InventorySlot[0], 0);

                items.Add(item);
                UpdateItem(item);
            }
        }

        private static IdentityType GetSlotType(int slot)
        {
            IdentityType slotType = IdentityType.Inventory;

            if (slot <= (int)EquipSlot.Weap_Hud2)
                slotType = IdentityType.WeaponPage;
            else if (slot <= (int)EquipSlot.Cloth_LeftFinger)
                slotType = IdentityType.ArmorPage;
            else if (slot <= (int)EquipSlot.Imp_Feet)
                slotType = IdentityType.ImplantPage;
            else if (slot <= (int)EquipSlot.Social_LeftWeap)
                slotType = IdentityType.SocialPage;

            return slotType;
        }

        internal static void UpdateItem(Item item)
        {
            if (item.UniqueIdentity == Identity.None)
                return;

            if (!FullUpdateProxy.Find(item.UniqueIdentity, out SimpleItem simpleItem))
                return;

            _items[_items.IndexOf(item)] = CreateUniqueItem(item.Slot, simpleItem);
        }

        private static Item CreateUniqueItem(Identity slot, SimpleItem simpleItem)
        {
            Item result = null;

            //We will create more variations of UniqueItem types as we need them
            if (simpleItem is WeaponItem)
            {
                result = new UniqueItem(slot, simpleItem);
            }
            else if (simpleItem is ChestItem)
            {
                result = new UniqueItem(slot, simpleItem);
            }
            else if (simpleItem is SimpleItem)
            {
                if (simpleItem.Stats.TryGetValue(Stat.TimeExist, out var remainingTime) && remainingTime > 0)
                {
                    result = new TempItem(slot, simpleItem);
                }
                else
                {
                    result = new UniqueItem(slot, simpleItem);
                }
            }

            if (result == null)
            {
                Logger.Error("UpdateItem Null Item. This shouldnt happen.");
                return null;
            }

            return result;
        }

        internal static void AddToNextAvailableSlot(Item item, bool updateContainer = true)
        {
            item.Slot = new Identity(IdentityType.Inventory, (int)GetNextAvailableSlot());

            if (updateContainer)
            {
                if (item.UniqueIdentity.Type == IdentityType.Container)
                    OnContainerUpdate(item.UniqueIdentity, new InventorySlot[0], 0);
            }

            _items.Add(item);
        }

        internal static void RemoveItem(Item item, bool wipeContainer = true)
        {
            _items.Remove(item);

            if (wipeContainer)
            {
                if (item.UniqueIdentity.Type == IdentityType.Container)
                {
                    if (!_containers.Find(item.UniqueIdentity, out Container container))
                        return;

                    _containers.Remove(container);
                }
            }
        }

        internal static void RemoveItem(Identity slot, out Item item)
        {
            item = _items.FirstOrDefault(x => x.Slot == slot);

            if (item != null)
                _items.Remove(_items.FirstOrDefault(x => x.Slot == slot));
        }

        internal static void RemoveContainerItem(Identity slot, out Container container, out Item item)
        {
            container = null;
            if (_containers.Find(slot, out item))
                _containers.RemoveItem(slot, out container);
        }

        internal static void RemoveBankItem(Identity slot, out Item item)
        {
            item = _items.FirstOrDefault(x => x.Slot == slot);
            Bank.RemoveItem(item);
        }

        internal static void RegisterLastItem(int lowId, int highId, int ql)
        {
            _lastProxyItem = new Item(Identity.None, Identity.None, lowId, highId, ql);
        }

        internal static void RegisterLastItem(SimpleItem simpleItem)
        {
            _lastProxyItem = CreateUniqueItem(Identity.None, simpleItem);
        }

        internal static void RegisterLastItem(Item item)
        {
            _lastProxyItem = item;
        }
    }
}