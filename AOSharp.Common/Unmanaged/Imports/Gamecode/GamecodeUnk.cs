using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class GamecodeUnk
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate int AppendSystemTextDelegate(int unk, [MarshalAs(UnmanagedType.LPStr)] string message, ChatColor color);
        public static AppendSystemTextDelegate AppendSystemText;

        [return: MarshalAs(UnmanagedType.U1)]
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public unsafe delegate bool IsInLineOfSightDelegate(IntPtr pThis, IntPtr pTarget);
        public static IsInLineOfSightDelegate IsInLineOfSight;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate void FollowTargetDelegate(IntPtr pVehicle_t, IntPtr pDynel, float distance, IntPtr waypoints);
        public static FollowTargetDelegate FollowTarget;
    }
}
