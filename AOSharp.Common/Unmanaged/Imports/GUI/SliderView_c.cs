using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class SliderView_c
    {
        [DllImport("GUI.dll", EntryPoint = "?GetValue@Slider_c@@UBE?AVVariant@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetValue(IntPtr pThis, IntPtr pVar);

        [DllImport("GUI.dll", EntryPoint = "?SetValue@Slider_c@@UAEXABVVariant@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetValue(IntPtr pThis, IntPtr pVar, bool unk);
    }
}
