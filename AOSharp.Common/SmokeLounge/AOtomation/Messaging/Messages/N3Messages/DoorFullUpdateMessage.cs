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

    [AoContract((int)N3MessageType.DoorFullUpdate)]
    public class DoorFullUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public DoorFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.DoorFullUpdate;
        }

        #endregion

        [AoMember(0)]
        public Identity Door { get; set; }

        [AoMember(1)]
        public int Unk1 { get; set; }

        [AoMember(2)]
        public Vector3 Position { get; set; }

        [AoMember(3)]
        public Quaternion Heading { get; set; }

        [AoMember(4)]
        public int Playfield { get; set; }

        //[AoMember(5)]
        //public int Unk2 { get; set; }

        //[AoMember(6)]
        //public int Unk3 { get; set; }

        //[AoMember(7)]
        //public int Unk4 { get; set; }

        //[AoMember(8, SerializeSize = ArraySizeType.X3F1)]
        //public GameTuple<Stat, uint>[] Skills { get; set; }

        //[AoMember(9)]
        //public int Unk5 { get; set; }

        //[AoMember(11)]
        //public int Unk7 { get; set; }

        //[AoMember(12)]
        //public int Unk8 { get; set; }

        //[AoMember(13, SerializeSize = ArraySizeType.X3F1)]
        //public Identity[] UnkIdentities { get; set; }
    }
}