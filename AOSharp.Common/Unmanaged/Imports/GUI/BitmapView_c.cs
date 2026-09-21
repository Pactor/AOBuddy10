using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class BitmapView_c
    {
        [DllImport("GUI.dll", EntryPoint = "?AddBitmap@BitmapView_c@@QAEXH@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetBitmap(IntPtr pThis, int id);

        [DllImport("GUI.dll", EntryPoint = "?Clear@BitmapView_c@@QAEX_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void Clear(IntPtr pThis, bool unk);
    }
}
