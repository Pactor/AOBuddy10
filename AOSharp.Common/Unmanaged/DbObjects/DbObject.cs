using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;

namespace AOSharp.Common.Unmanaged.DbObjects
{
    public abstract class DbObject
    {
        public IntPtr Pointer { get; }

        protected DbObject(IntPtr pointer)
        {
            Pointer = pointer;
        }
    }
}
