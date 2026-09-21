using System;
using System.Text;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class VLayoutNode_c
    {
        [DllImport("GUI.dll", EntryPoint = "??0VLayoutNode@@QAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        internal static extern IntPtr Constructor(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "??1VLayoutNode@@UAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Deconstructor(IntPtr pThis);

        public static IntPtr Create()
        {
            return Constructor(MSVCR100.New(0x2C));
        }
    }
}
