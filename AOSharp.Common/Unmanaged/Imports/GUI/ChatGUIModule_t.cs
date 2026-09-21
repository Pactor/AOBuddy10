using AOSharp.Common.Unmanaged.DataTypes;
using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class ChatGUIModule_t
    {
        [DllImport("GUI.dll", EntryPoint = "?GetInstanceIfAny@ChatGUIModule_c@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetInstance();

        [DllImport("GUI.dll", EntryPoint = "?ExpandChatTextArgs@ChatGUIModule_c@@SA?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@ABV23@@Z", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern IntPtr ExpandChatTextArgs(IntPtr pOut, IntPtr pMsg);

        //HandleGroupMessage
        [DllImport("GUI.dll", EntryPoint = "?HandleGroupMessage@ChatGUIModule_c@@AAEXPBUGroupMessage_t@Client_c@ppj@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void HandleGroupMessage(IntPtr pThis, IntPtr pGroupMessage);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate void DHandleGroupAction(IntPtr pThis, IntPtr pGroupMessage);
    }
}
