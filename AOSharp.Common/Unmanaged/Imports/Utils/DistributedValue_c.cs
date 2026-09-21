using AOSharp.Common.Unmanaged.DataTypes;
using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class DistributedValue_c
    {
        [DllImport("Utils.dll", EntryPoint = "?AddVariable@DistributedValue_c@@SAXABVString@@ABVVariant@@_N2@Z", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void AddVariable(IntPtr pName, IntPtr pVariant, bool unk1, bool unk2);

        [DllImport("Utils.dll", EntryPoint = "?GetDValue@DistributedValue_c@@SA?AVVariant@@ABVString@@_N@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetDValue(IntPtr pVariant, IntPtr pName, bool unk);

        [DllImport("Utils.dll", EntryPoint = "?SetDValue@DistributedValue_c@@SAXABVString@@ABVVariant@@@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetDValue(IntPtr pName, IntPtr pValue);

        [DllImport("Utils.dll", EntryPoint = "?SaveConfig@DistributedValue_c@@SA_NABV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@W4DValueCategory_e@@@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SaveConfig(IntPtr path, int category);

        [DllImport("Utils.dll", EntryPoint = "?LoadConfig@DistributedValue_c@@SA_NABV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@W4DValueCategory_e@@_N@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern int LoadConfig(IntPtr path, int category, bool unk);
    }
}
