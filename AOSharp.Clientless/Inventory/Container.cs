using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using System.Collections.Generic;
using System.Linq;

namespace AOSharp.Clientless
{
    public class Container
    {
        private const int INVENTORY_CAPACITY = 21;
        private const int INVENTORY_START = 0x0;
        public int NumFreeSlots => INVENTORY_CAPACITY - Items.Count();
        public int? NextAvailableSlot => Inventory.GetNextAvailableSlot(INVENTORY_START, INVENTORY_CAPACITY, Items);
        public bool IsFull => Items.Count == INVENTORY_CAPACITY;
        public bool IsOpen => Handle != 0;
        public Item Item => Inventory.Items.FirstOrDefault(x => x.UniqueIdentity == Identity);
        public int Handle { get; internal set; }
        public Identity Identity { get; internal set; }
        public List<Item> Items { get; internal set; }

        public Container(Identity identity, int handle)
        {
            Items = new List<Item>();
            Identity = identity;
            Handle = handle;
        }

        internal void RegisterItems(InventorySlot[] invSlots)
        {
            foreach (var slot in invSlots)
                Items.Add(new Item(new Identity(IdentityType.Backpack, GetHandleSlot(slot.Placement)),
                    slot.Identity, slot.ItemLowId, slot.ItemHighId, slot.Quality) { Count = slot.Count });
        }

        internal void AddItem(Item item)
        {
            item.Slot = new Identity(IdentityType.Backpack, GetHandleSlot((int)NextAvailableSlot));
            Items.Add(item);
        }

        private int GetHandleSlot(int slot) => (Handle << 16) | slot;
    }
}