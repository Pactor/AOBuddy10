using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class Connection_t
    {
        [DllImport("Connection.dll", EntryPoint = "?Send@Connection_t@@QAEHIIPBX@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Send(IntPtr pConnection, uint unk, int len, [In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] buf);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate int DSend(IntPtr pConnection, uint unk, int len, [In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] buf);
    }
}
