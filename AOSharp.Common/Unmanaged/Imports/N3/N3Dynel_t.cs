using AOSharp.Common.GameData;
using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class N3Dynel_t
    {
        [DllImport("N3.dll", EntryPoint = "??0n3Dynel_t@@IAE@ABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis, ref Identity identity);

        [DllImport("N3.dll", EntryPoint = "?GetDynel@n3Dynel_t@@SAPAV1@ABVIdentity_t@@@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetDynel(ref Identity identity);

        [DllImport("N3.dll", EntryPoint = "?GetDynelMap@n3Dynel_t@@SAABVDynelMap_t@@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetDynelMap();

        [DllImport("N3.dll", EntryPoint = "?SetRelRot@n3Dynel_t@@QAEXABVQuaternion_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetRelRot(IntPtr pThis, ref Quaternion rot);

        [DllImport("N3.dll", EntryPoint = "?GetZone@n3Dynel_t@@QBEPAVn3Zone_t@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetZone(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?GetGlobalPos@n3Dynel_t@@QBEABVVector3_t@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static unsafe extern Vector3* GetGlobalPos(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?SetCollPrimScale@n3Dynel_t@@IAEXM@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetCollPrimScale(IntPtr pThis, float radius);

        [DllImport("N3.dll", EntryPoint = "?SetVehicle@n3Dynel_t@@QAEXPAVVehicle_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetVehicle(IntPtr pThis, IntPtr pVehicle);
    }
}
