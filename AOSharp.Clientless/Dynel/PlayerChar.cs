using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOSharp.Clientless
{
    public class PlayerChar : SimpleChar
    {
        public Profession Profession => (Profession)GetStat(Stat.Profession);

        public PlayerChar(SimpleCharFullUpdateMessage simpleCharMsg) : base(simpleCharMsg)
        {
        }
    }
}
