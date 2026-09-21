using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class N3Camera_t
    {
        [DllImport("N3.dll", EntryPoint = "?IsFirstPerson@n3Camera_t@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsFirstPerson(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?StartZoomOut@n3Camera_t@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void StartZoomOut(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?StopZoomOut@n3Camera_t@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void StopZoomOut(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?StartZoomIn@n3Camera_t@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void StartZoomIn(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?StopZoomIn@n3Camera_t@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void StopZoomIn(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?ToggleCameraView@n3Camera_t@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void ToggleCameraView(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?GetTargetGlobalPos@CameraVehicle_t@@QBEABVVector3_t@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern Vector3 GetTargetGlobalPos();
    }
}
