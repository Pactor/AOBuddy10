using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class FlowControlModule_t
    {
        [DllImport("GUI.dll", EntryPoint = "?TeleportStartedMessage@FlowControlModule_t@@CAXXZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern void TeleportStartedMessage ();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate void DTeleportStartedMessage();

        public static unsafe bool* pIsTeleporting = (bool*)Kernel32.GetProcAddress(Kernel32.GetModuleHandle("GUI.dll"), "?m_isTeleporting@FlowControlModule_t@@2_NA");

        [DllImport("GUI.dll", EntryPoint = "?ActivateGameClosing@FlowControlModule_t@@CAXW4QuitStatus_e@1@@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ActivateGameClosing(int quitStatus);

        [DllImport("GUI.dll", EntryPoint = "?SetTimeLogoutStatus@FlowControlModule_t@@SAXW4QuitStatus_e@1@@Z")]
        public static extern void SetTimeLogoutStatus(int quitStatus);
    }
}
