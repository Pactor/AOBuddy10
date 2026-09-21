using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class TextInputView_c
    {
        [DllImport("GUI.dll", EntryPoint = "?SetText@TextInputView_c@@QAEXABV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetText(IntPtr pThis, IntPtr pStr);

        [DllImport("GUI.dll", EntryPoint = "?GetText@TextInputView_c@@QBEABV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetText(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?GetTextView@TextInputView_c@@QAEPAVTextView_c@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetTextView(IntPtr pTextInputView);
    }
}
