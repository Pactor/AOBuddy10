using System;
using System.Text;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class ListViewBaseItem_c
    {
        [DllImport("GUI.dll", EntryPoint = "?AppendChild@ListViewBaseItem_c@@QAEXPAV1@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void AppendChild(IntPtr pThis, IntPtr pItem);

        [DllImport("GUI.dll", EntryPoint = "?MakeSelectable@ListViewBaseItem_c@@QAEX_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void MakeSelectable(IntPtr pThis, bool selectable);
    }
}
