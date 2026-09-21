using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class ChatWindowNode_t
    {
        public static IntPtr ChatWindowController = Kernel32.GetProcAddress(Kernel32.GetModuleHandle("GUI.dll"), "?s_pcInstance@ChatGUIModule_c@@0PAV1@A") + 0x1C;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public unsafe delegate int AppendTextDelegate(IntPtr pThis, IntPtr pMsg, ChatColor color);
        public static AppendTextDelegate AppendText; 
    }
}
