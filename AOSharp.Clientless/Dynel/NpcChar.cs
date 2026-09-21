using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOSharp.Clientless
{
    public class NpcChar : SimpleChar
    {
        public Identity? Owner;

        public NpcChar(SimpleCharFullUpdateMessage simpleCharMsg) : base(simpleCharMsg)
        {
            Owner = simpleCharMsg.Owner;
        }
    }
}
