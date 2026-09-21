using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData.UI;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class Window_c
    {
        [DllImport("GUI.dll", EntryPoint = "??0Window@@QAE@ABVRect@@ABVString@@1W4WindowStyle_e@@I@Z", CallingConvention = CallingConvention.ThisCall)]
        internal static extern IntPtr Constructor(IntPtr pThis, ref Rect rect, IntPtr pNameStr, IntPtr pTitleStr, WindowStyle windowStyle, WindowFlags flags);

        [DllImport("GUI.dll", EntryPoint = "??1Window@@UAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Deconstructor(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?Show@Window@@QAEX_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void Show(IntPtr pThis, bool visible);

        [DllImport("GUI.dll", EntryPoint = "?Close@Window@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void Close(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?MoveToCenter@Window@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void MoveToCenter(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?MoveTo@Window@@QAEXMM@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void MoveTo(IntPtr pThis, float x, float y);

        [DllImport("GUI.dll", EntryPoint = "?GetMousePosition@Window@@QBE?AVPoint@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetMousePos(IntPtr pThis, ref Vector2 refPos);

        [DllImport("GUI.dll", EntryPoint = "?GetFrame@Window@@QBE?AVRect@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetFrame(IntPtr pThis, IntPtr pRect);

        [DllImport("GUI.dll", EntryPoint = "?GetScreenSize@Window@@SA?AVPoint@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetScreenSize(IntPtr pThis, ref Vector2 refPos);

        [DllImport("GUI.dll", EntryPoint = "?GetTabView@Window@@QBEPAVTabView@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetTabView(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?GetBounds@Window@@QBE?AVRect@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetBounds(IntPtr pThis, IntPtr pRect);

        [DllImport("GUI.dll", EntryPoint = "?AppendTab@Window@@QAEHABVString@@PAVView@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr AppendTab(IntPtr pThis, IntPtr pName, IntPtr pView);

        [DllImport("GUI.dll", EntryPoint = "?AddChild@Window@@QAEXPAVView@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr AppendChild(IntPtr pThis, IntPtr pView, bool unk);

        [DllImport("GUI.dll", EntryPoint = "?SetTitle@Window@@QAEXABVString@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetTitle(IntPtr pThis, IntPtr pTitle);

        [DllImport("GUI.dll", EntryPoint = "?ResizeTo@Window@@QAEXABVPoint@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void ResizeTo(IntPtr pThis, ref Vector2 size);

        [DllImport("GUI.dll", EntryPoint = "?SetSizeLimits@Window@@QAEXABVPoint@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetSizeLimits(IntPtr pThis, ref Vector2 minSize, ref Vector2 maxSize);

        [DllImport("GUI.dll", EntryPoint = "?FindView@Window@@QBEPAVView@@ABVString@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr FindView(IntPtr pThis, IntPtr viewName);

        [DllImport("GUI.dll", EntryPoint = "?FindWindowName@Window@@SAPAV1@PBD@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FindWindowName([MarshalAs(UnmanagedType.LPStr)] string windowName);

        [DllImport("GUI.dll", EntryPoint = "?SetAlpha@Window@@QAEXM@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetAlpha(IntPtr pThis, float value);

        [return: MarshalAs(UnmanagedType.U1)]

        [DllImport("GUI.dll", EntryPoint = "?IsVisible@Window@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsVisible(IntPtr pThis);

        public static unsafe IntPtr Create(Rect rect, string string1, string string2, WindowStyle style, WindowFlags flags)
        {
            StdString str1 = StdString.Create(string1);
            StdString str2 = StdString.Create(string2);

            IntPtr pWindow = Constructor(MSVCR100.New(0xAC), ref rect, str1.Pointer, str2.Pointer, style, flags);

            return pWindow;
        }
    }
}
