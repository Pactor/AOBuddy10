using System;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class RemoteFormat
    {
        [DllImport("ldb.dll", EntryPoint = "?ParseString@RemoteFormat@@SA?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@PBD@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ParseString(IntPtr pString, [MarshalAs(UnmanagedType.LPStr)] string remoteFormat);
    }
}
