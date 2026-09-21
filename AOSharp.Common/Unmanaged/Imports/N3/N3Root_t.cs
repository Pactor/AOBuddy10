using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class N3Root_t
    {
        [DllImport("N3.dll", EntryPoint = "?AddPlayfieldRoot@n3Root_t@@QAEXPAVn3Playfield_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void AddPlayfieldRoot(IntPtr pThis, IntPtr pPlayfield);
    }
}
