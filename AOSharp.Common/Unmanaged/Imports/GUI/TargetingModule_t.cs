using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class TargetingModule_t
    {
        [DllImport("GUI.dll", EntryPoint = "?GetInstanceIfAny@TargetingModule_t@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetInstanceIfAny();

        [DllImport("GUI.dll", EntryPoint = "?SetTarget@TargetingModule_t@@CAXABVIdentity_t@@_N@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetTarget(ref Identity target, bool unk);

        [DllImport("GUI.dll", EntryPoint = "?SelectSelf@TargetingModule_t@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SelectSelf(IntPtr pThis);
    }
}
