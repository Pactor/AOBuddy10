using System;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class LDBFace
    {
        [DllImport("ldb.dll", EntryPoint = "?GetText@LDBface@@SA?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@II@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetText(IntPtr pString, int type, int instance);
    }
}
