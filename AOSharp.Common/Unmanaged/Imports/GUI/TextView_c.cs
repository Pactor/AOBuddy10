using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class TextView_c
    {
        [DllImport("GUI.dll", EntryPoint = "?SetValue@TextView_c@@UAEXABVVariant@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetValue(IntPtr pThis, IntPtr pVariant, bool unk);

        [DllImport("GUI.dll", EntryPoint = "?SetText@TextView_c@@QAEXABV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetText(IntPtr pThis, IntPtr pStr);

        [DllImport("GUI.dll", EntryPoint = "?GetValue@TextView_c@@UBE?AVVariant@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetValue(IntPtr pThis, IntPtr pVariant);

        [DllImport("GUI.dll", EntryPoint = "?SetDefaultColor@TextView_c@@QAEXI@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetDefaultColor(IntPtr pThis, uint unk);
    }
}
