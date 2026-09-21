using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class GameTime_t
    {
        [DllImport("Gamecode.dll", EntryPoint = "?GetInstance@GameTime_t@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetInstance();

        [DllImport("Gamecode.dll", EntryPoint = "?GetNormalTime@GameTime_t@@QBENXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern double GetNormalTime(IntPtr pThis);
    }
}
