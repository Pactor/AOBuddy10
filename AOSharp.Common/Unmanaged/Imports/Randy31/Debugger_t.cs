using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class Debugger_t
    {
        [DllImport("Randy31.dll", EntryPoint = "?Get@Debugger_t@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetInstance();

        [DllImport("Randy31.dll", EntryPoint = "?AddLine@Debugger_t@@QAEXVVector3_t@@0MMM@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern int DrawLine(IntPtr pThis, float pos1X, float pos1Y, float pos1Z, float pos2X, float pos2Y, float pos2Z, float unk1, float unk2, float unk3);

        [DllImport("Randy31.dll", EntryPoint = "?AddSphere@Debugger_t@@QAEXVVector3_t@@MMMM@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern int DrawSphere(IntPtr pThis, float posX, float posY, float posZ, float radius, float unk1, float unk2, float unk3);
    }
}
