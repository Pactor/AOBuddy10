using AOSharp.Common.Unmanaged.DataTypes;
using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class MultiListViewItem_c
    {
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("GUI.dll", EntryPoint = "?IsSelected@MultiListViewItem_c@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsSelected(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?GetID@MultiListViewItem_c@@QBEABVVariant@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetID(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?GetListView@MultiListViewItem_c@@QBEPAVMultiListView_c@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetListView(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?Invalidate@MultiListViewItem_c@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void Invalidate(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?Select@MultiListViewItem_c@@QAEX_N0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void Select(IntPtr pThis, bool selected, bool unk);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate void DSelect(IntPtr pThis, bool selected, bool unk);

        [DllImport("GUI.dll", EntryPoint = "??0MultiListViewItem_c@@QAE@ABVVariant@@@Z", CallingConvention = CallingConvention.ThisCall)]
        internal static extern IntPtr Constructor(IntPtr pThis, IntPtr pVariant);

        [DllImport("GUI.dll", EntryPoint = "??1MultiListViewItem_c@@MAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Deconstructor(IntPtr pThis);

        public static IntPtr Create(Variant variant) => Constructor(MSVCR100.New(0x120), variant.Pointer);
    }

    public class LFTListViewItem_c
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public unsafe delegate IntPtr CreateLFTListViewItemDelegate(IntPtr pThis, int id, StdStringStruct str1, StdStringStruct str2, StdStringStruct str3, StdStringStruct str4, StdStringStruct str5);
        public static CreateLFTListViewItemDelegate CreateLFTListViewItem;

        /*
        public static IntPtr Create(int unk1, Identity dummyItem, bool unk2)
        {
            return CreateInventoryListViewItem(MSVCR100.New(0x120), unk1, ref dummyItem, unk2);
        }*/
    }
}
