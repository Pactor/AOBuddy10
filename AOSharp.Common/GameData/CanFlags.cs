using System;
using AOSharp.Common.Helpers;

namespace AOSharp.Common.GameData
{
    [Flags]
    public enum CanFlags : uint
    {
        None = BitFlag.None,
        Carry = BitFlag.Bit0,
        Sit = BitFlag.Bit1,
        Wear = BitFlag.Bit2,
        Use = BitFlag.Bit3,
        ConfirmUse = BitFlag.Bit4,
        Consume = BitFlag.Bit5,
        TutorChip = BitFlag.Bit6,
        TutorDevice = BitFlag.Bit7,
        BreakingAndEntering = BitFlag.Bit8,
        Stackable = BitFlag.Bit9,
        NoAmmo = BitFlag.Bit10,
        Burst = BitFlag.Bit11,
        FlingShot = BitFlag.Bit12,
        FullAuto = BitFlag.Bit13,
        AimedShot = BitFlag.Bit14,
        Bow = BitFlag.Bit15,
        ThrowAttack = BitFlag.Bit16,
        SneakAttack = BitFlag.Bit17,
        FastAttack = BitFlag.Bit18,
        DisarmTraps = BitFlag.Bit19,
        AutoSelect = BitFlag.Bit20,
        ApplyOnFriendly = BitFlag.Bit21,
        ApplyOnHostile = BitFlag.Bit22,
        ApplyOnSelf = BitFlag.Bit23,
        CantSplit = BitFlag.Bit24,
        Brawl = BitFlag.Bit25,
        Dimach = BitFlag.Bit26,
        EnableHandAttractors = BitFlag.Bit27,
        CanBeWornWithSocialArmor = BitFlag.Bit28,
        CanParryRiposite = BitFlag.Bit29,
        CanBeParriedRiposited = BitFlag.Bit30,
        ApplyOnFightingTarget = BitFlag.Bit31,
    }
}
