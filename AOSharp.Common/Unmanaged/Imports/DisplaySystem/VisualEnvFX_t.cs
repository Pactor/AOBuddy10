using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class VisualEnvFX_t
    {
        [DllImport("DisplaySystem.dll", EntryPoint = "?FrameProcess@VisualEnvFX_t@@QAEXMMIMAAVVector3_t@@AAVQuaternion_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern int FrameProcess(IntPtr pThis, float unk1, float unk2, int unk3, float unk4, int unk5, int unk6);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate int DFrameProcess(IntPtr pThis, float unk1, float unk2, int unk3, float unk4, int unk5, int unk6);

        [DllImport("DisplaySystem.dll", EntryPoint = "?GetInstance@VisualEnvFX_t@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetInstance();

        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleOcclusionCulling@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleOcclusionCulling(IntPtr pVisualEnvFX);

        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerDepthDisplay@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerDepthDisplay(IntPtr pVisualEnvFX);

        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerDoNotRender@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerDoNotRender(IntPtr pVisualEnvFX);

        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerKDTreeDisplay@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerKDTreeDisplay(IntPtr pVisualEnvFX);

        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerOcclusionBodies@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerOcclusionBodies(IntPtr pVisualEnvFX);

        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerOcclusionScan@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerOcclusionScan(IntPtr pVisualEnvFX);

        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerOcclusionTest@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerOcclusionTest(IntPtr pVisualEnvFX);

        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerOffscreenDisplay@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerOffscreenDisplay(IntPtr pVisualEnvFX);

        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerRefractionDisplay@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerRefractionDisplay(IntPtr pVisualEnvFX);

        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerShowCATWireframe@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerShowCATWireframe(IntPtr pVisualEnvFX);

        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerShowGroundWireframe@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerShowGroundWireframe(IntPtr pVisualEnvFX);

        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerShowLiquidWireframe@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerShowLiquidWireframe(IntPtr pVisualEnvFX);
        
        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerShowMouseFix@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerShowMouseFix(IntPtr pVisualEnvFX);
        
        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerShowStatelWireframe@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerShowStatelWireframe(IntPtr pVisualEnvFX);
        
        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerShowSurfaceSliding@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerShowSurfaceSliding(IntPtr pVisualEnvFX);
        
        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerSphereTest@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerSphereTest(IntPtr pVisualEnvFX);
        
        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerSyncDisplay@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerSyncDisplay(IntPtr pVisualEnvFX);
        
        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerToggleCapOffscreen@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerToggleCapOffscreen(IntPtr pVisualEnvFX);
        
        [DllImport("DisplaySystem.dll", EntryPoint = "?ToggleRandyDebuggerToggleCapRefraction@VisualEnvFX_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool ToggleRandyDebuggerToggleCapRefraction(IntPtr pVisualEnvFX);

    }
}
