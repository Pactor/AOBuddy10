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
    public class FullUpdateProxy
    {
        private static List<SimpleItemOwner> _simpleItemOwners = new List<SimpleItemOwner>();

        internal static bool Find(Identity identity, out SimpleItem simpleItem)
        {
            simpleItem = null;

            foreach (var proxy in _simpleItemOwners.SelectMany(x=>x.Dynels))
            {
                if (proxy.Identity != identity)
                    continue;

                simpleItem = proxy;
                return true;
            }

            return false;
        }

        internal static bool GetSimpleItems(Identity ownerIdentity, out List<SimpleItem> items) => (items = _simpleItemOwners.FirstOrDefault(x => x.Owner == ownerIdentity).Dynels) != null;

        internal static void OnSIFU(SimpleItemFullUpdateMessage sifu)
        {
            Identity owner = new Identity((IdentityType)sifu.OwnerType, sifu.OwnerInstance);

            if (!Find(owner, out SimpleItemOwner proxyOwner))
            {
                proxyOwner = new SimpleItemOwner(owner);
                _simpleItemOwners.Add(proxyOwner);
            }

            int lowId = sifu.Stats.FirstOrDefault(x => x.Value1 == Stat.ACGItemTemplateID).Value2;
            int highId = sifu.Stats.FirstOrDefault(x => x.Value1 == Stat.ACGItemTemplateID2).Value2;
            int ql = sifu.Stats.FirstOrDefault(x => x.Value1 == Stat.ACGItemLevel).Value2;

            SimpleItem simpleItem = new SimpleItem(sifu.Identity, sifu.Position, sifu.Rotation, sifu.Stats.ToDict());

            proxyOwner.Dynels.Add(simpleItem);
            Inventory.RegisterLastItem(simpleItem);

            //Logger.Information($"SIFU {sifu.Identity}");
        }

        internal static void OnCFU(ChestFullUpdateMessage cfu)
        {
            if (!Find(cfu.Owner, out SimpleItemOwner proxyOwner))
            {
                proxyOwner = new SimpleItemOwner(cfu.Owner);
                _simpleItemOwners.Add(proxyOwner);
            }

            int lowId = cfu.Stats.FirstOrDefault(x => x.Value1 == Stat.ACGItemTemplateID).Value2;
            int highId = cfu.Stats.FirstOrDefault(x => x.Value1 == Stat.ACGItemTemplateID2).Value2;
            int ql = cfu.Stats.FirstOrDefault(x => x.Value1 == Stat.ACGItemLevel).Value2;

            ChestItem chestItem = new ChestItem(cfu.Identity, cfu.Stats.ToDict());

            proxyOwner.Dynels.Add(chestItem);
            Inventory.RegisterLastItem(chestItem);

            //Logger.Information($"CFU {cfu.Identity}");
        }

        internal static void OnWIFU(WeaponItemFullUpdateMessage wifu)
        {
            if (!Find(wifu.Owner, out SimpleItemOwner proxyOwner))
            {
                proxyOwner = new SimpleItemOwner(wifu.Owner);
                _simpleItemOwners.Add(proxyOwner);
            }

            int lowId = wifu.Stats.FirstOrDefault(x => x.Value1 == Stat.ACGItemTemplateID).Value2;
            int highId = wifu.Stats.FirstOrDefault(x => x.Value1 == Stat.ACGItemTemplateID2).Value2;
            int ql = wifu.Stats.FirstOrDefault(x => x.Value1 == Stat.ACGItemLevel).Value2;

            WeaponItem weaponItem = new WeaponItem(wifu.Identity, wifu.Stats.ToDict());

            Inventory.RegisterLastItem(weaponItem);
            proxyOwner.Dynels.Add(weaponItem);
            //Logger.Information($"WIFU {wifu.Identity}");
        }

        internal static void Reset() => _simpleItemOwners = new List<SimpleItemOwner>();

        internal static bool Find(Identity identity, out SimpleItemOwner owner) => (owner = _simpleItemOwners.FirstOrDefault(x => x.Owner == identity)) != null;
    }

    internal class SimpleItemOwner
    {
        public Identity Owner;
        public List<SimpleItem> Dynels;

        public SimpleItemOwner(Identity owner)
        {
            Owner = owner;
            Dynels = new List<SimpleItem>();
        }
    }
}