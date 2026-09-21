using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class TabView_c
    {
        [DllImport("GUI.dll", EntryPoint = "?GetTabCount@TabView@@QBEHXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int GetTabCount(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?AppendTab@TabView@@QAEHABVString@@PAVView@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr AppendTab(IntPtr pThis, IntPtr pName, IntPtr pView);
    }
}
