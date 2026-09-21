using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class InventoryGUIModule_c
    {
        [DllImport("GUI.dll", EntryPoint = "?GetInstanceIfAny@InventoryGUIModule_c@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetInstance();

        [DllImport("GUI.dll", EntryPoint = "?GetBackpackName@InventoryGUIModule_c@@QAE?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@ABVIdentity_t@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetBackpackName(IntPtr pThis, IntPtr pStr, ref Identity identity, bool unk);

        [DllImport("GUI.dll", EntryPoint = "?SetBackpackName@InventoryGUIModule_c@@QAEXABVIdentity_t@@ABV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern int SetBackpackName(IntPtr pThis, ref Identity identity, IntPtr pStr, bool unk);

        [DllImport("GUI.dll", EntryPoint = "?SlotContainerOpened@InventoryGUIModule_c@@AAEXABVIdentity_t@@_N1@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void ContainerOpened(IntPtr pThis, ref Identity identity, bool unk, bool unk2);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate void DContainerOpened(IntPtr pThis, ref Identity identity, bool unk, bool unk2);
    }
}
