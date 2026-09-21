using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class Variant_c
    {
        [DllImport("Utils.dll", EntryPoint = "?AsInt32@Variant@@QBEJXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int AsInt32(IntPtr pThis);

        [DllImport("Utils.dll", EntryPoint = "?AsFloat@Variant@@QBEMXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern float AsFloat(IntPtr pThis);

        [DllImport("Utils.dll", EntryPoint = "?AsDouble@Variant@@QBENXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern double AsDouble(IntPtr pThis);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Utils.dll", EntryPoint = "?AsBool@Variant@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool AsBool(IntPtr pThis);

        [DllImport("Utils.dll", EntryPoint = "?AsString@Variant@@QBE?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr AsString(IntPtr pThis, IntPtr pStr);

        [DllImport("Utils.dll", EntryPoint = "?SetBool@Variant@@QAEX_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetBool(IntPtr pThis, bool value);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Utils.dll", EntryPoint = "?SaveToString@Variant@@QAE_NAAV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool SaveToString(IntPtr pThis, IntPtr pStr);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Utils.dll", EntryPoint = "?LoadFromString@Variant@@QAE_NPBD@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool LoadFromString(IntPtr pThis, [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport("Utils.dll", EntryPoint = "??0Variant@@QAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis);

        [DllImport("Utils.dll", EntryPoint = "??0Variant@@QAE@H@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis, int value);

        [DllImport("Utils.dll", EntryPoint = "??0Variant@@QAE@M@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis, float value);

        [DllImport("Utils.dll", EntryPoint = "??0Variant@@QAE@N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis, double value);

        [DllImport("Utils.dll", EntryPoint = "??0Variant@@QAE@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis, bool value);

        [DllImport("Utils.dll", EntryPoint = "??1Variant@@QAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int Deconstructor(IntPtr pThis);
    }
}
