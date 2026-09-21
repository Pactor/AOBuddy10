using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Common.GameData
{
    public enum GroupMessageType : byte
    {
        Tower = 0xA,
        Org = 0x3,
        Team = 0x82,
        Shopping = 0x86,
        OOC = 0x87,
    }
}