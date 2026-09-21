using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class CheckBox_c
    {
        [DllImport("GUI.dll", EntryPoint = "??0CheckBox_c@@QAE@ABVString@@0_N1@Z", CallingConvention = CallingConvention.ThisCall)]
        internal static extern IntPtr Constructor(IntPtr pThis, IntPtr string1, IntPtr string2, bool defaultValue, bool horizontalSpacer);

        [DllImport("GUI.dll", EntryPoint = "??1CheckBox_c@@UAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Deconstructor(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?GetValue@CheckBox_c@@UBE?AVVariant@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetValue(IntPtr pThis, IntPtr unk);

        [DllImport("GUI.dll", EntryPoint = "?SetValue@CheckBox_c@@UAEXABVVariant@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetValue(IntPtr pThis, IntPtr pVariant, bool unk);

        [DllImport("GUI.dll", EntryPoint = "?SlotButtonToggled@CheckBox_c@@AAEX_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SlotButtonToggled(IntPtr pThis, bool enabled);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate void DSlotButtonToggled(IntPtr pThis, bool enabled);

        public static IntPtr Create(string name, string text, bool defaultValue, bool horizontalSpacer)
        {
            IntPtr pNew = MSVCR100.New(0x158);
            StdString nameStr = StdString.Create(name);
            StdString textStr = StdString.Create(text);
            IntPtr pView = Constructor(pNew, nameStr.Pointer, textStr.Pointer, defaultValue, horizontalSpacer);

            return pView;
        }
    }
}
