using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.Imports.DatabaseController;
using AOSharp.Common.Unmanaged.Interfaces;

namespace AOSharp.Common.Unmanaged.DbObjects
{
    public class PlayfieldDistrictInfo : DbObject
    {
        public PlayfieldDistrictInfoMemStruct MemStruct;
        public int ZoneCount;
        
        public static PlayfieldDistrictInfo Get(int instance)
        {
            DBIdentity identity = new DBIdentity(DBIdentityType.PlayfieldDistrictInfo, instance);
            return ResourceDatabase.GetDbObject<PlayfieldDistrictInfo>(identity);
        }

        internal unsafe PlayfieldDistrictInfo(IntPtr pointer) : base(pointer)
        {
            MemStruct = *(PlayfieldDistrictInfoMemStruct*)pointer;
            ZoneCount = (MemStruct.ZoneToDistrictMapLast - MemStruct.ZoneToDistrictMapFirst) / 2;
        }
        
        public DistrictData GetDistrictData(uint zone)
        {
            return new DistrictData(PlayfieldDistrictInfo_t.GetDistrictData(Pointer, zone));
        }
    }

    public struct PlayfieldDistrictInfoMemStruct
    {
        public int Offset0 { get; set; }
        public int Offset4 { get; set; }
        public int Offset8 { get; set; }
        public int OffsetC { get; set; }
        public int Offset10 { get; set; }
        public int Offset14 { get; set; }
        public int Offset18 { get; set; }
        public int Offset1C { get; set; }
        public int Offset20 { get; set; }
        public int Offset24 { get; set; }
        public int ZoneToDistrictMapFirst { get; set; }
        public int ZoneToDistrictMapLast { get; set; }
        public int Offset30 { get; set; }
        public int Offset34 { get; set; }
    }
}
