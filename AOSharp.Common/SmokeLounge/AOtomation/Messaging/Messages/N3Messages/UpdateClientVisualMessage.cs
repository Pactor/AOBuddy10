// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UpdateClientVisualMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the UpdateClientVisualMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging
    // (Messages\N3Messages\UpdateClientVisualMessage.cs); no changes.

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.UpdateClientVisual)]
    public class UpdateClientVisualMessage : N3Message
    {
        public UpdateClientVisualMessage()
        {
            this.N3MessageType = N3MessageType.UpdateClientVisual;
        }

        [AoMember(0)]
        public int HeadMesh { get; set; }

        [AoMember(1)]
        public byte Race { get; set; }

        [AoMember(2)]
        public byte Breed { get; set; }

        [AoMember(3)]
        public byte Sex { get; set; }
    }
}
