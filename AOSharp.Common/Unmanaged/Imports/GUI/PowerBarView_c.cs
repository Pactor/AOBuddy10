using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class PowerBarView_c
    {
        [DllImport("GUI.dll", EntryPoint = "?SetValue@PowerbarView_c@@UAEXABVVariant@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetValue(IntPtr pThis, IntPtr pVariant, bool unk);

        [DllImport("GUI.dll", EntryPoint = "?GetValue@PowerbarView_c@@UBE?AVVariant@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetValue(IntPtr pThis, IntPtr pVariant);
        
        [DllImport("GUI.dll", EntryPoint = "?SetLabel@PowerbarView_c@@QAEXABVString@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetLabel(IntPtr pThis, IntPtr text);
        
        [DllImport("GUI.dll", EntryPoint = "?SetLabels@PowerbarView_c@@QAEXABVString@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetLabels(IntPtr pThis, IntPtr left, IntPtr right);
        
        [DllImport("GUI.dll", EntryPoint = "?SetBarColor@PowerbarView_c@@QAEXI@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetBarColor(IntPtr pThis, uint color);
    }
}
