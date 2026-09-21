// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CorpseFullUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CorpseFullUpdate type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
    using System;
    using System.Collections.Generic;

    [AoContract((int)N3MessageType.CorpseFullUpdate)]
    public class CorpseFullUpdateMessage : N3Message
    {
        private DateTime receivedAt;

        #region Constructors and Destructors

        public CorpseFullUpdateMessage()
        {
            N3MessageType = N3MessageType.CorpseFullUpdate;
            receivedAt = DateTime.Now;
        }

        public DateTime DecayTime
        {
            get
            {
                int timeExist = 18000;
                int deadTimer = 60;

                foreach (var tuple in Stats)
                {
                    if (tuple.Value1 == Stat.TimeExist)
                        timeExist = tuple.Value2;

                    if (tuple.Value1 == Stat.DeadTimer)
                        deadTimer = tuple.Value2;
                }

                return receivedAt.AddMinutes((double)timeExist / deadTimer / 100);
            }
        }

        [AoMember(0)]
        public int Unknown1 { get; set; }

        [AoMember(1)]
        public int Unknown2 { get; set; }

        [AoMember(2)]
        public Identity Owner { get; set; }

        [AoMember(3)]
        public Vector3 Position { get; set; }

        [AoMember(4)]
        public Quaternion Heading { get; set; }

        [AoMember(5)]
        public int PlayfieldId { get; set; }

        [AoMember(6)]
        public Identity StateMachine { get; set; }

        [AoMember(7)]
        public short Unknown3 { get; set; }

        [AoMember(8, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<Stat, int>[] Stats { get; set; }

        [AoMember(10, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        [AoMember(11)]
        public int Unknown4 { get; set; }

        [AoMember(12)]
        public int Unknown5 { get; set; }

        [AoMember(12, SerializeSize = ArraySizeType.X3F1)]
        public int[] UnknownArray { get; set; }

        [AoMember(13)]
        public int Unknown6 { get; set; }

        [AoMember(14, SerializeSize = ArraySizeType.X3F1)]
        public AnimationEffect[] AnimationEffects { get; set; }

        [AoMember(15)]
        public Identity UnknownIdentity { get; set; }

        [AoMember(16, SerializeSize = ArraySizeType.X3F1)]
        public Texture[] Textures { get; set; }

        #endregion
    }
}