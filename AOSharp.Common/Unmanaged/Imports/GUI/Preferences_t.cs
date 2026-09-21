using System;
using System.Text;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class Preferences_t
    {
        [DllImport("GUI.dll", EntryPoint = "?GetInstanceIfAny@Preferences_t@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetInstanceIfAny();

        [DllImport("GUI.dll", EntryPoint = "?GetCharacterPath@Preferences_t@@QAEPBDXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetCharacterPath(IntPtr pThis);
    }
}
