using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.Imports.GameData
{
    public class LandControlMap_t
    {
        [DllImport("GameData.dll", EntryPoint = "?GetBitmapData@LandControlMap_t@GameData@@QBEPBXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetBitmapData(IntPtr pLandControlMap);

        [DllImport("GameData.dll", EntryPoint = "?IsTilePlaceAble@LandControlMap_t@GameData@@IBE_NHH@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsTilePlaceAble(IntPtr pLandControlMap, int x, int z);
        
        [DllImport("GameData.dll", EntryPoint = "?CanPlaceInTileAt@LandControlMap_t@GameData@@QBE_NABVVector3_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool CanPlaceInTileAt(IntPtr pLandControlMap, ref Vector3 pos);

        [DllImport("GameData.dll", EntryPoint = "?IsTileAtAllowedBorder@LandControlMap_t@GameData@@QBE_NABVVector3_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsTileAtAllowedBorder(IntPtr pLandControlMap, ref Vector3 pos);
    }
}
