using System;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class StringListViewItem_c
    {
        [DllImport("GUI.dll", EntryPoint = "??0StringListViewItem_c@@QAE@ABVVariant@@ABVString@@HH@Z", CallingConvention = CallingConvention.ThisCall)]
        internal static extern IntPtr Constructor(IntPtr pThis, IntPtr pVariant, IntPtr pName, int unk1, int unk2);

        [DllImport("GUI.dll", EntryPoint = "??1StringListViewItem_c@@UAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Deconstructor(IntPtr pThis);

        public static IntPtr Create(Variant variant, string name, int unk1, int unk2)
        {
            StdString nameStr = StdString.Create(name);
            IntPtr pView = Constructor(MSVCR100.New(0x98), variant.Pointer, nameStr.Pointer, unk1, unk2);

            return pView;
        }
    }
}
