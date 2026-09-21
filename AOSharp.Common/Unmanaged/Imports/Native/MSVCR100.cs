using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class MSVCR100
    {
        [DllImport("MSVCR100.dll", EntryPoint = "??2@YAPAXI@Z", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr New(int size);

        [DllImport("MSVCR100.dll", EntryPoint = "??3@YAXPAX@Z", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Delete(IntPtr pointer);
    }
}
