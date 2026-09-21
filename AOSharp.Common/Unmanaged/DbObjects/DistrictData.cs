using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;

namespace AOSharp.Common.Unmanaged.DbObjects
{
    public class DistrictData : DbObject
    {
        public DistrictData(IntPtr pointer) : base(pointer)
        {
            
        }

        public FightMode GetFightMode()
        {
            return (FightMode) DistrictData_t.GetFightMode(Pointer);
        }

        public bool IsLandControlled()
        {
            return DistrictData_t.IsLandControlled(Pointer);
        }
    }
}
