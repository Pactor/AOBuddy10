using System;
using AOSharp.Common.Helpers;

namespace AOSharp.Common.GameData
{
    public enum Profession : uint
    {
        Unknown = 0,
        Soldier = 1,
        MartialArtist = 2,
        Engineer = 3,
        Fixer = 4,
        Agent = 5,
        Adventurer = 6,
        Trader = 7,
        Bureaucrat = 8,
        Enforcer = 9,
        Doctor = 10,
        NanoTechnician = 11,
        Metaphysicist = 12,
        Monster = 13,
        Keeper = 14,
        Shade = 15
    }

    [Flags]
    public enum ProfessionFlag : uint
    {
        None = BitFlag.None,
        Soldier = BitFlag.Bit1,
        MartialArtist = BitFlag.Bit2,
        Engineer = BitFlag.Bit3,
        Fixer = BitFlag.Bit4,
        Agent = BitFlag.Bit5,
        Adventurer = BitFlag.Bit6,
        Trader = BitFlag.Bit7,
        Bureaucrat = BitFlag.Bit8,
        Enforcer = BitFlag.Bit9,
        Doctor = BitFlag.Bit10,
        NanoTechnician = BitFlag.Bit11,
        MetaPhysicist = BitFlag.Bit12,
        Keeper = BitFlag.Bit14,
        Shade = BitFlag.Bit15
    }
}
