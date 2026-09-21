using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Common.Unmanaged.Imports.GUI
{
    public class BrowserModule_c
    {
        [DllImport("GUI.dll", EntryPoint = "?GetInstance@BrowserModule_c@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetInstance();

        [DllImport("GUI.dll", EntryPoint = "?SlotBrowserWindow@BrowserModule_c@@AAEX_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SlotBrowserWindow(IntPtr pThis, bool hidden = false);
    }
}
