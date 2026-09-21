using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class DummyItem_t
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate int GetStatDelegate(IntPtr pThis, Stat stat, int detail);
        public static GetStatDelegate GetStat;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate IntPtr GetSpellListDelegate(IntPtr pThis, SpellListType spellList);
        public static GetSpellListDelegate GetSpellList;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate IntPtr GetSpellDataUnkDelegate(IntPtr pThis, ref GetSpellDataUnkStruct pUnkStruct);
        public static GetSpellDataUnkDelegate GetSpellDataUnk;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate IntPtr GetSpellDataDelegate(ref GetSpellDataUnkStruct pThis);
        public static GetSpellDataDelegate GetSpellData;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr GetOpNameDelegate(int op);
        public static GetOpNameDelegate GetOpName;

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct GetSpellDataUnkStruct
        {
            public IntPtr SpellListPointer;
            public int Unk;
            public int Idx;
        }
    }
}
