using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\FullAutoMessage.cs); no
    // changes.

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.FullAuto)]
    public class FullAutoMessage:N3Message
    {
        public FullAutoMessage()
        {
            this.N3MessageType = N3MessageType.FullAuto;
        }

        [AoMember(1)]
        public int BulletsHit { get; set; }

        [AoMember(2)]
        public int Reserved { get; set; }

    }
}
