using AOSharp.Common.Unmanaged.DataTypes;
using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class Looper
    {
        [DllImport("Utils.dll", EntryPoint = "?GetName@Looper@@QBE?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetName(IntPtr pLooper, IntPtr pName);
    }
}
