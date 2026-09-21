using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class DropdownMenu_c
    {
        [DllImport("GUI.dll", EntryPoint = "?AppendItem@DropdownMenu_c@@QAEIABVVariant@@ABVString@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern int AppendItem(IntPtr pThis, IntPtr pVar, IntPtr pLabelStr);

        [DllImport("GUI.dll", EntryPoint = "?GetSelection@DropdownMenu_c@@QBEHXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern uint GetSelection(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?GetItemLabel@DropdownMenu_c@@QBEABVString@@I@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetItemLabel(IntPtr pThis, uint num);

        [DllImport("GUI.dll", EntryPoint = "?SelectByIndex@DropdownMenu_c@@QAEXH_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr SelectByIndex(IntPtr pThis, uint num, bool unk);

        [DllImport("GUI.dll", EntryPoint = "?DeleteItem@DropdownMenu_c@@QAEXI@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr DeleteItem(IntPtr pThis, uint num);
    }
}
