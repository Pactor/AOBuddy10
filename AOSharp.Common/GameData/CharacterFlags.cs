using System;

namespace AOSharp.Common.GameData
{
    [Flags]
    public enum CharacterFlags
    {
        None = 0x00,
        Unknown = 0x01,
        Unknown1 = 0x08,
        Unknown2 = 0x40,
        //Tower = 0x184,
        PetTower = 0x200,
        Unknown4 = 0x800,
        Unknown5 = 0x1000,
        Tower = 0x20000,
        CollideWithStatels = 0x80000,
        Unknown7 = 0x100000,
        HasItemsForSale = 0x200000,
        HasVisibleName = 0x400000,
        HasBlueName = 0x800000,
        Pet = 0x8000000,
        Unknown8 = 0x20000000,
    }

    [Flags]
    public enum DeathFlags
    {
        None = 0x00,
        DeathWhiteScreen = 0x20,
        Dying1 = 0x30,
        Dying2 = 0x60,
        Dying3 = 0x70,
        Unknown200 = 0x200
    }

    public enum NpcClan
    {
        EngineerAttackPet = 95,
        MPHealPets = 96,
        MPAttackPets = 97,
        MPMezzPets = 98,
        ShadowMutants = 150
    }
    public enum NpcFamily
    {
        AttackPet = 100001,
        HealPet = 110001,
        SupportPet = 120001,
        Vendor = 11001,
        GuardsA = 70001,
        GuardsB = 70002
    }

    public enum AppearanceFlags
    {
        None = 0,
        HelmetVisible = 0x4,
        RightPadVisible = 0x01,
        LeftPadVisible = 0x02,
        AllowDoubleLeftPads = 0x08,
        AllowDoubleRightPads = 0x10,
        SocialTabEnabled = 0x20,
        SocialTabOnly = 0x40
    }

    [Flags]
    public enum ExpansionFlags : int
    {
        NotumWars = 1 << 0,
        ShadowLands = 1 << 1,
        ShadowLandsPreOrder = 1 << 2,
        AlienInvasion = 1 << 3,
        AlienInvasionPreOrder = 1 << 4,
        LostEden = 1 << 5,
        LostEdenPreOrder = 1 << 6,
        LegacyOfTheXan = 1 << 7,
        LegacyOfTheXanPreOrder = 1 << 8,
        Mail = 1 << 9,
        PMVObsidianEdition = 1 << 10
    }
}
