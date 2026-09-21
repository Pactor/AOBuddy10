using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class ListViewBase_c
    {
        [DllImport("GUI.dll", EntryPoint = "??0ListViewBase_c@@QAE@ABVRect@@ABVString@@HII@Z", CallingConvention = CallingConvention.ThisCall)]
        internal static extern unsafe IntPtr Constructor(IntPtr pThis, Rect* rect, IntPtr pName, int unk1, int unk2, int unk3);

        [DllImport("GUI.dll", EntryPoint = "??1ListViewBase_c@@UAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Deconstructor(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?AppendItem@ListViewBase_c@@QAEXPAVListViewBaseItem_c@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void AppendItem(IntPtr pThis, IntPtr pItem);

        public static unsafe IntPtr Create(Rect rect, string name, int unk1, int unk2, int unk3)
        {
            StdString nameStr = StdString.Create(name);
            IntPtr pView = Constructor(MSVCR100.New(0x190), &rect, nameStr.Pointer, unk1, unk2, unk3);

            return pView;
        }
    }
}
