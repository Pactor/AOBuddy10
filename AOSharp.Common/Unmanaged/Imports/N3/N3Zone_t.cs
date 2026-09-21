using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class N3Zone_t
    {
        [DllImport("N3.dll", CharSet = CharSet.Ansi, EntryPoint = "?LoadSurface@n3Zone_t@@QAEXPAVCellSurface_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void LoadSurface(IntPtr pThis, IntPtr pSurface);

        [DllImport("N3.dll", EntryPoint = "?GetSurface@n3Zone_t@@QBEPBVSurface_i@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetSurface(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?GetInstance@n3Zone_t@@QBEIXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int GetInstance(IntPtr pThis);
    }
}
