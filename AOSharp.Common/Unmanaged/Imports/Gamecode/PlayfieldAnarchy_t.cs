using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class PlayfieldAnarchy_t
    {
        [DllImport("Gamecode.dll", EntryPoint = "?GetNumberOfWaters@PlayfieldAnarchy_t@@QAEHXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int GetNumberOfWaters(IntPtr pThis);

        [DllImport("Gamecode.dll", EntryPoint = "?GetWaters@PlayfieldAnarchy_t@@QAEPAUn3WaterData_t@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetWaters(IntPtr pThis);

        [DllImport("Gamecode.dll", EntryPoint = "?AreVehiclesAllowed@PlayfieldAnarchy_t@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool AreVehiclesAllowed(IntPtr pThis);

        [DllImport("Gamecode.dll", EntryPoint = "?IsShadowlandPF@PlayfieldAnarchy_t@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsShadowlandPF(IntPtr pThis);

        [DllImport("Gamecode.dll", EntryPoint = "GetDistrictInfo@PlayfieldAnarchy_t@@QAEPAVPlayfieldDistrictInfo_t@GameData@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetDistrictInfo(IntPtr pThis);

        [DllImport("Gamecode.dll", EntryPoint = "?GetLandControlMap@PlayfieldAnarchy_t@@QBEPBVLandControlMap_t@GameData@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetLandControlMap(IntPtr pThis);
        
        [DllImport("Gamecode.dll", EntryPoint = "?GetPFWorldXPos@PlayfieldAnarchy_t@@QBEHXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int GetPFWorldXPos(IntPtr pThis);
        
        [DllImport("Gamecode.dll", EntryPoint = "?GetPFWorldZPos@PlayfieldAnarchy_t@@QBEHXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int GetPFWorldZPos(IntPtr pThis);
        
        [DllImport("Gamecode.dll", EntryPoint = "?GetSafePos@PlayfieldAnarchy_t@@UBE?AVVector3_t@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern Vector3 GetSafePos(IntPtr pThis);
        
        [DllImport("Gamecode.dll", EntryPoint = "?IsGrid@PlayfieldAnarchy_t@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsGrid(IntPtr pThis);

        [DllImport("N3.dll", EntryPoint = "?AddChildDynel@n3Playfield_t@@QAEXPAVn3Dynel_t@@ABVVector3_t@@ABVQuaternion_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void AddChildDynel(IntPtr pThis, IntPtr pDynel, ref Vector3 pos, ref Quaternion rot);

        [DllImport("Gamecode.dll", EntryPoint = "?AddCellMonitor@PlayfieldAnarchy_t@@QAEXABVVector3_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void AddCellMonitor(IntPtr pThis, ref Vector3 pos);

        [DllImport("N3.dll", EntryPoint = "?CalculateWaterHeightMax@n3Playfield_t@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void CalculateWaterHeightMax(IntPtr pThis);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?Run@PlayfieldAnarchy_t@@UAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool Run(IntPtr pThis);

    }
}
