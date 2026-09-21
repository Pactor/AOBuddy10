using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports.GameData;
using AOSharp.Common.Unmanaged.Interfaces;

namespace AOSharp.Common.Unmanaged.DbObjects
{
    public class LandControlMap : DbObject
    {
        public LandControlMapMemStruct MemStruct;

        public unsafe LandControlMap(IntPtr pointer) : base(pointer)
        {
            MemStruct = *(LandControlMapMemStruct*)pointer;
        }

        public static LandControlMap Get(int playfieldId)
        {
            DBIdentity identity = new DBIdentity(DBIdentityType.LandControlMap, playfieldId);
            return ResourceDatabase.GetDbObject<LandControlMap>(identity);
        }
        
        public byte[] GetBitmapData()
        {
            IntPtr pBitmapData = LandControlMap_t.GetBitmapData(Pointer);

            if (pBitmapData == IntPtr.Zero)
                return Array.Empty<byte>();
            
            int length = (MemStruct.NumTilesX * MemStruct.NumTilesZ) / 8;
            byte[] bitmapDataBytes = new byte[length];
            Marshal.Copy(pBitmapData, bitmapDataBytes, 0, length);

            return bitmapDataBytes;
        }

        public bool IsTilePlaceAble(int x, int z)
        {
            return LandControlMap_t.IsTilePlaceAble(Pointer, x, z);
        }

        public bool CanPlaceInTileAt(Vector3 pos)
        {
            return LandControlMap_t.CanPlaceInTileAt(Pointer, ref pos);
        }

        public bool IsTileAtAllowedBorder(Vector3 pos)
        {
            return LandControlMap_t.IsTileAtAllowedBorder(Pointer, ref pos);
        }
    }
    
    public struct LandControlMapMemStruct
    {
        public int Offset0 { get; set; }
        public int Offset4 { get; set; }
        public Identity Identity { get; set; }
        public int Offset10 { get; set; }
        public int Offset14 { get; set; }
        public int Version { get; set; }
        public int NumTilesX { get; set; }
        public int NumTilesZ { get; set; }
        public int Offset24 { get; set; }
    }
}
