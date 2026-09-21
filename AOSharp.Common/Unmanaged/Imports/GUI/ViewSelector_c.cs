using System;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class ViewSelector_c
    {
        [DllImport("GUI.dll", EntryPoint = "??0ViewSelector_c@@QAE@ABVRect@@VString@@HII@Z", CallingConvention = CallingConvention.ThisCall)]
        internal static extern unsafe IntPtr Constructor(IntPtr pThis, Rect* rect, IntPtr pName, int garbage1, int garbage2, int garbage3, int garbage4, int garbage5, int garbage6, int unk1, int unk2, int unk3);

        [DllImport("GUI.dll", EntryPoint = "??1ViewSelector_c@@UAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Deconstructor(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?SetListView@ViewSelector_c@@QAEXPAVListViewBase_c@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetListView(IntPtr pThis, IntPtr pListViewBase);

        [DllImport("GUI.dll", EntryPoint = "?GetListView@ViewSelector_c@@QBEPAVListViewBase_c@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetListView(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?AppendView@ViewSelector_c@@QAEXPAVView@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void AppendView(IntPtr pThis, IntPtr pView);

        [DllImport("GUI.dll", EntryPoint = "?SetValue@ViewSelector_c@@UAEXABVVariant@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetValue(IntPtr pThis, IntPtr pVar, bool unk);

        public static unsafe IntPtr Create(Rect rect, string name, int unk1, int unk2, int unk3)
        {
            StdString nameStr = StdString.Create(name);
            IntPtr pView = Constructor(MSVCR100.New(0x178), &rect, nameStr.Pointer, 0, 0, 0, 0, 0, 0, unk1, unk2, unk3);
  
            return pView;
        }
    }
}
