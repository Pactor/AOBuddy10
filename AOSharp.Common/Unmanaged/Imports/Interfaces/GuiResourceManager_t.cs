using AOSharp.Common.GameData;
using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class GuiResourceManager_t
    {
        [DllImport("Interfaces.dll", EntryPoint = "?GetInstance@GuiResourceManager_t@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetInstance();

        [DllImport("Interfaces.dll", EntryPoint = "?GetGuiTexture@GuiResourceManager_t@@QAEPAVSpriteInfo_t@@HPBDW4Format_e@@II@Z", CallingConvention = CallingConvention.ThisCall)]
        internal static extern IntPtr GetGuiTexture(IntPtr pThis, int gfxId, [MarshalAs(UnmanagedType.LPStr)] string file, int format, int unk1, int unk2);

        [DllImport("Interfaces.dll", EntryPoint = "?ReleaseTexture@GuiResourceManager_t@@QAEXPAVSpriteInfo_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        internal static extern void ReleaseTexture(IntPtr pThis, IntPtr pSprite);
    }
}
