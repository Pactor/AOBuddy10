// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BankMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the BankMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// What is in the character's own bank.
    /// </summary>
    /// <remarks>
    /// Ported from OmniCell. A container and nothing else. The reader at Gamecode 0x10072518 pushes
    /// the container and the stream and calls the shared container reader at
    /// 0x1002A5DB - the same one FullCharacter's inventory and BankCorpse use -
    /// and then stops. The dispatcher at 0x1007254B resolves the message identity to a
    /// character and writes that character's own instance into the container.
    /// </remarks>
    [AoContract((int)N3MessageType.Bank)]
    public class BankMessage : N3Message
    {
        #region Constructors and Destructors

        public BankMessage()
        {
            this.N3MessageType = N3MessageType.Bank;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// What is in it.
        /// </summary>
        /// <remarks>
        /// Each entry is the shared container record: a placement, two int16s,
        /// an Identity and a GameData::ACGItem_t.
        /// </remarks>
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public InventorySlot[] Contents { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is Contents.")]
        public InventorySlot[] BankSlots { get => this.Contents; set => this.Contents = value; }

        #endregion
    }
}
