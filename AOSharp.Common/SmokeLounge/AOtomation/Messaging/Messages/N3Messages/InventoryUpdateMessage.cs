// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InventoryUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the InventoryUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.InventoryUpdate)]
    public class InventoryUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public InventoryUpdateMessage()
        {
            this.N3MessageType = N3MessageType.InventoryUpdate;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int Unknown1 { get; set; }

        [AoMember(1)]
        public int Unknown2 { get; set; }

        [AoMember(2, SerializeSize = ArraySizeType.X3F1)]
        public InventorySlot[] Items { get; set; }

        [AoMember(3)]
        public Identity InventoryIdentity { get; set; }

        [AoMember(4)]
        public int Handle { get; set; }

        [AoMember(5)]
        public int Unknown3 { get; set; }

        #endregion
    }
}