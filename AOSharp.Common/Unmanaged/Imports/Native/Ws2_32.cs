using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class Ws2_32
    {
        [DllImport("ws2_32.dll")]
        public unsafe static extern int recv(int socket, IntPtr buffer, int len, int flags);
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public unsafe delegate int RecvDelegate(int socket, IntPtr buffer, int len, int flags);

        [DllImport("ws2_32.dll", CallingConvention = CallingConvention.StdCall)]
        public static unsafe extern int send(int socket, IntPtr buffer, int len, int flags);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        public delegate int SendDelegate(int socket, IntPtr buffer, int len, int flags);
    }
}
