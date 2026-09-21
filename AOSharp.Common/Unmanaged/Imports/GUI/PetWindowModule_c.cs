using AOSharp.Common.GameData;
using System;
using System.Runtime.InteropServices;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class PetWindowModule_c
    {
        [DllImport("GUI.dll", EntryPoint = "?GetInstance@PetWindowModule_c@@SAPAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetInstance();

        [DllImport("GUI.dll", EntryPoint = "?GetPetListMap@PetWindowModule_c@@AAEPAV?$map@HVIdentity_t@@U?$less@H@std@@V?$allocator@U?$pair@$$CBHVIdentity_t@@@std@@@3@@std@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetPetListMap(IntPtr pThis);

        [DllImport("GUI.dll", EntryPoint = "?GetPetID@PetWindowModule_c@@QAE?AVIdentity_t@@H@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetPetID(IntPtr pThis, ref Identity identity, byte idx);
    }
}
