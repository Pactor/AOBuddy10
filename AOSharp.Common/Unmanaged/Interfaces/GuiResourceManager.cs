using System;
using System.IO;
using AOSharp.Common.GameData;
using AOSharp.Common.Helpers;
using AOSharp.Common.Unmanaged.Imports;

namespace AOSharp.Common.Unmanaged.Interfaces
{
    public class GuiResourceManager
    {
        public static IntPtr GetGUITexture(int gfxId, string path, int format = 2, int unk1 = 0, int unk2 = 0)
        {
            return GuiResourceManager_t.GetGuiTexture(GuiResourceManager_t.GetInstance(), gfxId, path, format, unk1, unk2);
        }

        public static void ReleaseTexture(IntPtr pSprite)
        {
            GuiResourceManager_t.ReleaseTexture(GuiResourceManager_t.GetInstance(), pSprite);
        }

        public static IntPtr CreateGUITexture(string name, int id, string path)
        {
            if (!File.Exists(path))
                return IntPtr.Zero;

            DynamicID.Add(name, id);
            return GetGUITexture(id, path);
        }
    }
}
