using AOSharp.Common.GameData;
using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.DataTypes
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ACEDataBlock
    {
        [FieldOffset(0x8)]
        public int BlockSize;

        [FieldOffset(0x14)]
        public IntPtr Data;
    }
}
