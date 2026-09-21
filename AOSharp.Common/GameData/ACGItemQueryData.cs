using System;
using System.Text;
using System.Runtime.InteropServices;

namespace AOSharp.Common.GameData
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ACGItemQueryData
    {
        public int LowId;
        public int HighId;
        public int QL;
    }
}
