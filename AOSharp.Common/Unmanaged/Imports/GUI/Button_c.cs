using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class Button_c
    {
        [DllImport("GUI.dll", EntryPoint = "?GetLabel@Button_c@@QAEABVString@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetLabel(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?SetLabel@Button_c@@QAEXABVString@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetLabel(IntPtr pThis, IntPtr pStr);

        [DllImport("GUI.dll", EntryPoint = "?SetGfx@Button_c@@QAEXW4StateID_e@1@H@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetGfx(IntPtr pThis, ButtonState buttonState, int gfxId);

        [DllImport("GUI.dll", EntryPoint = "?SetBackgroundIcon@Button_c@@QAEXH_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetBackgroundIcon(IntPtr pThis, int id, bool state);

        [DllImport("GUI.dll", EntryPoint = "?SetColorOverride@Button_c@@QAEXI@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetColorOverride(IntPtr pThis, uint unk);

        [DllImport("GUI.dll", EntryPoint = "?SetLabelColor@Button_c@@QAEXI@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetLabelColor(IntPtr pThis, uint unk);

        [DllImport("GUI.dll", EntryPoint = "?GetBorderView@Button_c@@QAEPAVBorderView_c@@W4StateID_e@1@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetBorderView(IntPtr pThis, ButtonState buttonState);
    }
}
