using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class ItemListViewBase_c
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public unsafe delegate IntPtr CreateItemListViewDelegate(IntPtr pThis, ref Rect rect, int flags, int unk3, int unk4, ref Identity unk5);
        public static CreateItemListViewDelegate CreateItemListView;

        public static IntPtr Create(Rect rect, int flags, int unk3, int unk4, Identity unk5)
        {
            return CreateItemListView(MSVCR100.New(0x370), ref rect, flags, unk3, unk4, ref unk5);
        }
    }

    public class InventoryListViewItem_c
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public unsafe delegate IntPtr CreateInventoryListViewItemDelegate(IntPtr pThis, int unk1, ref Identity dummyItem, bool unk2);
        public static CreateInventoryListViewItemDelegate CreateInventoryListViewItem;

        public static IntPtr Create(int unk1, Identity dummyItem, bool unk2)
        {
            return CreateInventoryListViewItem(MSVCR100.New(0x120), unk1, ref dummyItem, unk2);
        }
    }
}
