using System;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class N3InfoItemRemote_t
    {
        [DllImport("N3.dll", EntryPoint = "?KeyToString@n3InfoItemRemote_t@@SAABV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@J@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr KeyToString(int key);
    }
}
