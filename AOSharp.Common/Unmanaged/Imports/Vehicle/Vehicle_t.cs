using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public static class Vehicle_t
    {
        [DllImport("Vehicle.dll", EntryPoint = "?EnableFalling@Vehicle_t@@QAEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void EnableFalling(IntPtr pThis);

        [DllImport("Vehicle.dll", EntryPoint = "?SetMaxVel@Vehicle_t@@QAEXM@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetMaxVel(IntPtr pThis, float maxVel);

        [DllImport("Vehicle.dll", EntryPoint = "?SetBody@Vehicle_t@@QAEXPAVVehicleBody_i@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetBody(IntPtr pThis, IntPtr pBody);
    }
}
