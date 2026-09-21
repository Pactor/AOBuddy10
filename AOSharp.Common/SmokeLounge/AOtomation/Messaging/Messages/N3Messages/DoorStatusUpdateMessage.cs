// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DoorFullUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the DoorFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.DoorStatusUpdate)]
    public class DoorStatusUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public DoorStatusUpdateMessage()
        {
            this.N3MessageType = N3MessageType.DoorStatusUpdate;
        }

        #endregion

        [AoMember(0)]
        public int Unknown1 { get; set; }

        [AoMember(1)]
        public int Unknown2 { get; set; }

        [AoMember(2)]
        public byte Unknown3 { get; set; }

        [AoMember(3)]
        public short Unknown4 { get; set; }

        [AoMember(4, SerializeSize = ArraySizeType.X3F1)]
        public int UnknownArray { get; set; }
    }
}