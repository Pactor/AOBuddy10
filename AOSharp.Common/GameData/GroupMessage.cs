using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.Unmanaged.DataTypes;

namespace AOSharp.Common.GameData
{
    public class GroupMessage
    {
        public readonly IntPtr Pointer;

        public GroupMessage(IntPtr pointer)
        {
            Pointer = pointer;
        }

        public unsafe string SenderName => (*(MemStruct*)Pointer).SenderName.ToString();
        public unsafe uint SenderId => (*(MemStruct*)Pointer).SenderId;

        public unsafe int ChannelIdMaybe => (*(MemStruct*)Pointer).ChannelIdMaybe;

        public unsafe int ChannelType => (*(MemStruct*)Pointer).ChannelType;

        [StructLayout(LayoutKind.Explicit, Pack = 0)]
        private struct MemStruct
        {
            [FieldOffset(0x0)]
            public int ChannelIdMaybe;

            [FieldOffset(0x04)]
            public int ChannelType;

            [FieldOffset(0x08)]
            public uint SenderId;

            [FieldOffset(0xC)]
            public IntPtr SenderName;
        }
    }
}
