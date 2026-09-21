using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class RadioButtonGroup_c
    {
        [DllImport("GUI.dll", EntryPoint = "??0RadioButtonGroup_c@@QAE@ABVRect@@ABVString@@HII@Z", CallingConvention = CallingConvention.ThisCall)]
        internal static extern IntPtr Constructor(IntPtr pThis, IntPtr pName, int unk1, uint unk2, uint unk3);

        [DllImport("GUI.dll", EntryPoint = "?GetValue@RadioButtonGroup_c@@UBE?AVVariant@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetValue(IntPtr pThis, IntPtr pVariant);

        [DllImport("GUI.dll", EntryPoint = "?SetValue@RadioButtonGroup_c@@UAEXABVVariant@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetValue(IntPtr pThis, IntPtr pVariant, bool unk);

        public static IntPtr Create(string name, int unk1, uint unk2, uint unk3)
        {
            IntPtr pNew = MSVCR100.New(0x250);
            StdString nameStr = StdString.Create(name);
            IntPtr pView = Constructor(pNew, nameStr.Pointer, unk1, (uint)pNew, (uint)pNew);

            return pView;
        }
    }
}
