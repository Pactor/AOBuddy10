using System;
using System.Text;
using System.Runtime.InteropServices;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace AOSharp.Common.GameData
{
    public enum IdentityType
    {
        None = 0x00,
        WeaponPage = 0x65,
        ArmorPage = 0x66,
        ImplantPage = 0x67,
        Inventory = 0x68,
        BankByRef = 0x69,
        Reclaim = 0x6A,
        Backpack = 0x6B,
        Contract = 0x6D,
        KnuBotTradeWindow = 0x6C,
        OverflowWindow = 0x6E,
        TradeWindow = 0x6F,
        SocialPage = 0x73,
        ShopInventory = 0x767,
        PlayerShopInventory = 0x790,
        PlayfieldUnk = 0x9C43,
        Playfield2 = 0x9C50,
        SimpleChar = 0xC350,
        CityController = 0xC418,
        Terminal = 0xC73D,
        Door = 0xC748,
        Container = 0xC749,
        WeaponInstance = 0xC74A,
        VendingMachine = 0xC75B,
        TempBag = 0xC767,
        Corpse = 0xC76A,
        MissionKey = 0xC76D,
        MissionKeyDuplicator = 0xC76E,
        MailTerminal = 0xC773,
        ProxyInstance = 0x0000C77D,
        DummyItem = 0xC788,
        PerkHash = 0xC78E,
        Battlestation = 0xC794,
        PlayfieldProxy = 0xC79C,
        Playfield = 0xC79D,
        ACGBuildingGeneratorData = 0x0000C79F,
        NanoProgram = 0xCF1B,
        GfxEffect = 0xCF26,
        MissionTerminal = 0xDAC1,
        Mission = 0xDAC3,
        ACGEntrance = 0xDAC6,
        TeamWindow = 0xDEA9,
        Organization = 0xDEAA,
        Bank = 0xDEAD,
        SpecialAction = 0xDEB0,
        MobHash = 0x111D3,
        Playfield3 = 0x186A1
    }

    public enum DBIdentityType : int
    {
        RDBPlayfield = 1000001,
        Texture = 1010004,
        LandControlMap = 1000008,
        RDBTilemap = 1000009,
        InfoObject = 1000010,
        SurfaceResource = 1000013,
        PlayfieldDistrictInfo = 1000014,
        Mesh = 1010001,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Identity
    {
        [AoMember(0)]
        public IdentityType Type { get; set; }

        [AoMember(1)]
        public int Instance { get; set; }

        public static Identity None
        {
            get { return new Identity(IdentityType.None, 0); }
        }

        public Identity(IdentityType type, int instance)
        {
            Type = type;
            Instance = instance;
        }

        public Identity(int instance)
        {
            Type = IdentityType.SimpleChar;
            Instance = instance;
        }

        public override string ToString()
        {
            if (Type == IdentityType.MobHash)
                return string.Format("({0}:{1})", Type, Encoding.ASCII.GetString(BitConverter.GetBytes(Instance)));

            return string.Format("({0}:{1})", Type, Instance.ToString("X4"));
        }
        public static bool operator == (Identity identity1, IdentityType Type)
        {
            return identity1 != null && identity1.TypeEquals(Type);
        }

        public static bool operator != (Identity identity1, IdentityType Type)
        {
            return !identity1.TypeEquals(Type);
        }

        public static bool operator == (Identity identity1, Identity identity2)
        {
            return identity1.Equals(identity2);
        }

        public static bool operator != (Identity identity1, Identity identity2)
        {
            return !identity1.Equals(identity2);
        }

        public bool TypeEquals(object obj)
        {
            return (obj is IdentityType) && Type.Equals((IdentityType)obj);
        }

        public override bool Equals(object obj)
        {
            return (obj is Identity) && Type.Equals(((Identity)obj).Type)
                   && Instance.Equals(((Identity)obj).Instance);
        }

        public override int GetHashCode()
        {
            var hashCode = 17;
            hashCode = (23 * hashCode) + Type.GetHashCode();
            hashCode = (23 * hashCode) + Instance.GetHashCode();
            return hashCode;
        }
    }

    public struct DBIdentity
    {
        public DBIdentityType Type;
        public int Instance;

        public DBIdentity(DBIdentityType type, int instance)
        {
            Type = type;
            Instance = instance;
        }
    }
}
