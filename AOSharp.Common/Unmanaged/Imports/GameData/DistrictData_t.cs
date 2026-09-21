using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class DistrictData_t
    {
        [DllImport("GameData.dll", EntryPoint = "?GetFightMode@DistrictData_t@GameData@@QBE?AW4FightTypeAllowed_e@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int GetFightMode(IntPtr pDistrictData);

        [DllImport("GameData.dll", EntryPoint = "?IsLandControlled@DistrictData_t@GameData@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsLandControlled(IntPtr pDistrictData);
    }
}
