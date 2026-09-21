using AOSharp.Common.GameData;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.DataTypes
{
    [StructLayout(LayoutKind.Sequential, Size = 0x18)]
    public struct PlayfieldProxy
    {
        public Identity ProxyId;
        public Identity Unknown;
        public Identity PlayfieldId;
    }
}
