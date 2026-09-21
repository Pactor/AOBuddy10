using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Interfaces;

namespace AOSharp.Common.Unmanaged.DbObjects
{
    class RDBPlayfield : DbObject
    {
        public RDBPlayfield(IntPtr pointer) : base(pointer)
        {
            
        }

        public static RDBPlayfield Get(int playfieldId)
        {
            DBIdentity identity = new DBIdentity(DBIdentityType.RDBPlayfield, playfieldId);
            return ResourceDatabase.GetDbObject<RDBPlayfield>(identity);
        }
    }
}
