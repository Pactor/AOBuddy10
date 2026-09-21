using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class WeaponHolder_t
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate IntPtr GetWeaponDelegate(IntPtr pThis, EquipSlot slot, int unk);
        public static GetWeaponDelegate GetWeapon;
 
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        public delegate bool IsDynelInWeaponRangeDelegate(IntPtr pThis, IntPtr pWeapon, IntPtr pDynel);
        public static IsDynelInWeaponRangeDelegate IsDynelInWeaponRange;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate byte IsInRangeDelegate(IntPtr pThis);
        public static IsInRangeDelegate IsInRange;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate IntPtr GetDummyWeaponDelegate(IntPtr pThis, Stat stat);
        public static GetDummyWeaponDelegate GetDummyWeapon;
    }
}
