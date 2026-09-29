using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\ActionMessage.cs).
    // Changes for this library only: AOSharp's Identity type; Action and Instigator are nullable, because this
    // library's flag-gated read hands back null when the gate is shut (FieldMask without bit 0) and a non-nullable
    // member would throw unboxing it.
    // AOBuddy census 2026-09-29: 38 copies in the bot's mission recordings and 444 in the retail dumps, every one read
    // to the last byte by this class.

    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.Action)]
    public class ActionMessage : N3Message
    {
        public ActionMessage()
        {
            this.N3MessageType = N3MessageType.Action;
        }

        [AoMember(1)]
        [AoFlags("flag")]
        public int FieldMask { get; set; }

        [AoUsesFlags("flag", typeof(int), FlagsCriteria.HasAny, new[] { 1 })]
        [AoMember(2)]
        public int? Action { get; set; }

        [AoUsesFlags("flag", typeof(Identity), FlagsCriteria.HasAny, new[] { 1 })]
        [AoMember(3)]
        public Identity? Instigator { get; set; }

    }
}
