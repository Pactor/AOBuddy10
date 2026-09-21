using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public static class XMLObject_c
    {
        [DllImport("Utils.dll", EntryPoint = "?LoadXMLObject@XMLObject_c@@SAPAV1@ABVString@@0@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr LoadXMLObject(IntPtr pPathStr, IntPtr pUnkStr);
    }
}
