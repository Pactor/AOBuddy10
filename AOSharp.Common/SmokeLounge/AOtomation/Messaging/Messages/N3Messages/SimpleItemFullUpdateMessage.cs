// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleItemFullUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SimpleItemFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.SimpleItemFullUpdate)]
    public class SimpleItemFullUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public SimpleItemFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.SimpleItemFullUpdate;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int Unknown1 { get; set; }

        [AoFlags("OwnerType")]
        [AoMember(1)]
        public int OwnerType { get; set; }

        [AoMember(2)]
        public int OwnerInstance { get; set; }

        [AoUsesFlags("OwnerType", typeof(Vector3), FlagsCriteria.EqualsToAny, new[] { 0 })]
        [AoMember(3)]
        public Vector3? Position { get; set; }

        [AoUsesFlags("OwnerType", typeof(Quaternion), FlagsCriteria.EqualsToAny, new[] { 0 })]
        [AoMember(4)]
        public Quaternion? Rotation { get; set; }

        [AoMember(5)]
        public int PlayfieldId { get; set; }

        [AoMember(6)]
        public Identity StateMachine { get; set; }

        [AoMember(7)]
        public short Unknown2 { get; set; }

        [AoMember(8, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<Stat, int>[] Stats { get; set; }

        #endregion
    }
}