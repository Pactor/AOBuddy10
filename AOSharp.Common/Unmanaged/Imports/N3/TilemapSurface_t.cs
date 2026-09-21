using AOSharp.Common.GameData;
using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class TilemapSurface_t
    {
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("N3.dll", EntryPoint = "?GetLineIntersection@n3TilemapSurface_t@@UBE_NABVVector3_t@@0AAV2@1_NPAVLocalitySource_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool GetLineIntersection(IntPtr pThis, ref Vector3 pos1, ref Vector3 pos2, ref Vector3 hitPos, ref Vector3 hitNormal, byte unk, IntPtr plocalitySource);
    }
}
