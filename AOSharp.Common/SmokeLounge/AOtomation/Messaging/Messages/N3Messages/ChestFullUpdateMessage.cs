// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChestFullUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ChestFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A container: a chest on the ground (a mission's find-item containers, loot) or a bag a character holds.
    /// </summary>
    /// <remarks>
    /// The layout is OmniCell's ChestItemFullUpdateMessage, which reads all 40,263 ChestFullUpdates in the bot's
    /// mission recordings with no byte left over (audit 2026-09-29). The old layout had no position: for the 39,504
    /// chests on the ground it read X as PlayfieldId and the heading's first zero word as the stats count (-1), and
    /// threw. The position and heading come only when the owner instance is 0 (ground chests); a held bag has
    /// neither. Stats (always six): 0 Flags (0x40 = locked, set on 4,979), 23 StaticInstance, 701 ACGItemLevel
    /// (the QL), 702/703 ACGItemTemplateID/ID2, 412 MultipleCount.
    /// </remarks>
    [AoContract((int)N3MessageType.ChestFullUpdate)]
    public class ChestFullUpdateMessage : N3Message
    {
        private int identityType;
        private int instance;

        public ChestFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.ChestFullUpdate;
        }

        /// <summary>Who holds it: a character for a bag, (0, 0) for a chest on the ground.</summary>
        public Identity Owner { get; set; }

        [AoMember(1)]
        public int MsgVersion { get; set; }

        [AoMember(2)]
        public int Identitytype
        {
            get { return this.identityType; }
            set { this.identityType = value; this.Owner = new Identity((IdentityType)value, this.instance); }
        }

        [AoMember(3)]
        [AoFlags("flag")]
        public int Instance
        {
            get { return this.instance; }
            set { this.instance = value; this.Owner = new Identity((IdentityType)this.identityType, value); }
        }

        [AoMember(4)]
        [AoUsesFlags("flag", typeof(Vector3), FlagsCriteria.HasNone, new[] { int.MaxValue })]
        public Vector3? Coordinates { get; set; }

        [AoMember(5)]
        [AoUsesFlags("flag", typeof(Quaternion), FlagsCriteria.HasNone, new[] { int.MaxValue })]
        public Quaternion? Heading { get; set; }

        [AoMember(6)]
        public int PlayfieldId { get; set; }

        [AoMember(7)]
        public Identity StateMachine { get; set; }

        [AoMember(8)]
        public byte InventoryId { get; set; }

        [AoMember(9)]
        public byte BodyLocation { get; set; }

        [AoMember(10, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<Stat, int>[] Stats { get; set; }

        [AoMember(11, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        [AoMember(12)]
        public int TailVersion { get; set; }

        /// <summary>Stat 299: what a lock pick is up against (44-114 on the wire).</summary>
        [AoMember(13)]
        public int LockDifficulty { get; set; }

        [AoMember(14, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] Keyholders { get; set; }

        [AoMember(15)]
        public int TailEndVersion { get; set; }
    }
}
