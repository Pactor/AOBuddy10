using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Imports.DatabaseController;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class PlayfieldDistrictInfo_t
    {
        [DllImport("GameData.dll", EntryPoint = "?GetDistrictData@PlayfieldDistrictInfo_t@GameData@@QBEPBVDistrictData_t@2@I@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetDistrictData(IntPtr pPlayfieldDistrictInfo, uint unk1);
    }
}
