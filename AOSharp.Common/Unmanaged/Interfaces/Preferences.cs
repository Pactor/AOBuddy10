using System;
using AOSharp.Common.GameData;
using AOSharp.Common.Helpers;
using AOSharp.Common.Unmanaged.Imports;

namespace AOSharp.Common.Unmanaged.Interfaces
{
    public class Preferences
    {
        public static string GetCharacterPath()
        {
            IntPtr pPreferences = Preferences_t.GetInstanceIfAny();

            if (pPreferences == IntPtr.Zero)
                return null;
            
            return Utils.UnsafePointerToString(Preferences_t.GetCharacterPath(pPreferences));
        }
    }
}
