using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class ComboBox_c
    {
        [DllImport("GUI.dll", EntryPoint = "?AppendItem@ComboBox_c@@QAEIABVVariant@@ABVString@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern int AppendItem(IntPtr pThis, IntPtr pVar, IntPtr pLabelStr);

        [DllImport("GUI.dll", EntryPoint = "?Clear@ComboBox_c@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Clear(IntPtr pThis);
    }
}
