using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using SmokeLounge.AOtomation.Messaging.GameData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Clientless
{
    public class Bank
    {
        private const int INVENTORY_CAPACITY = 104;
        private const int INVENTORY_START = 0;
        private const int INVENTORY_END = INVENTORY_START + INVENTORY_CAPACITY;
        public int NumFreeSlots => INVENTORY_CAPACITY - _items.Count();
        public int? NextAvailableSlot => Inventory.GetNextAvailableSlot(INVENTORY_START, INVENTORY_END, _items);
        public bool IsFull => NextAvailableSlot == null;
        public bool IsOpen;
        private List<Item> _items;
        public IReadOnlyList<Item> Items => _items;
        public Action Opened;

        internal Bank()
        {
            IsOpen = false;
            _items = new List<Item>();
        }

        internal void RegisterItems(InventorySlot[] invSlots)
        {
            foreach (var slot in invSlots)
            {
                //Logger.Information($"Registering Bank Item: {slot.ItemLowId} @ {slot.Placement}");

                _items.Add(new Item(new Identity(IdentityType.BankByRef, slot.Placement), slot.Identity, slot.ItemLowId, slot.ItemHighId, slot.Quality));
            }

            Opened?.Invoke();
        }

        internal void AddItemToAvailableSlot(Item item)
        {
            //Logger.Information($"Adding {item.Id} to bank in slot {NextAvailableSlot.Value}");
            item.Slot = new Identity(IdentityType.BankByRef, NextAvailableSlot.Value);
            _items.Add(item);
        }

        internal void RemoveItem(Item item)
        {
            _items.Remove(item);
        }
    }
}