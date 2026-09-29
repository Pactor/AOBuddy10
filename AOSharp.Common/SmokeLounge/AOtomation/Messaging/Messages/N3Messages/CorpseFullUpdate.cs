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
    using System.Linq;

    /// <summary>
    /// What is left where something died.
    /// </summary>
    /// <remarks>
    /// Completed 2026-09-29 against OmniCell's Messages\N3Messages\CorpseFullUpdateMessage.cs and
    /// Serialization\Serializers\Custom\CorpseFullUpdateSerializer.cs, which read every captured corpse with zero
    /// bytes left. The AOSharp original stopped after <see cref="Textures"/>, leaving 4 bytes (no meshes) or
    /// 4 + 4 + 44 per mesh unread on every corpse, read the keyholder list as int32s instead of identities, and
    /// read the effect list as a fixed 60-byte struct that only fits game function 53031 with no criteria.
    ///
    /// Wire order (OmniCell names in brackets where the AOSharp name differs): int MsgVersion [Unknown1], int
    /// ItemVersion [Unknown2], Identity holder [Owner], Vector3, Quaternion, int Playfield, Identity StateMachine,
    /// short InventoryId+BodyLocation [Unknown3], X3F1 Stats, Int32-counted Name, int LockableVersion [Unknown4],
    /// int LockDifficulty [Unknown5], X3F1 Identity[] Keyholders, int ChestVersion [Unknown6], X3F1 NanoEffect[],
    /// Identity Owner = the dead dynel [UnknownIdentity], X3F1 Texture[], int HasMeshes, and - only when HasMeshes
    /// is non-zero - X3F1 CorpseMesh[]. Where HasMeshes is 0 the message ends with no array header at all.
    /// </remarks>
    [AoContract((int)N3MessageType.CorpseFullUpdate)]
    public class CorpseFullUpdateMessage : N3Message
    {
        private DateTime receivedAt;

        #region Constructors and Destructors

        public CorpseFullUpdateMessage()
        {
            N3MessageType = N3MessageType.CorpseFullUpdate;
            receivedAt = DateTime.Now;
            Keyholders = new Identity[0];
            NanoEffects = new NanoEffectRecord[0];
            Meshes = new CorpseMesh[0];
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

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The message version (OmniCell MsgVersion; 8 in every capture).
        /// </summary>
        [AoMember(0)]
        public int Unknown1 { get; set; }

        /// <summary>
        /// The item-family version (OmniCell ItemVersion).
        /// </summary>
        [AoMember(1)]
        public int Unknown2 { get; set; }

        /// <summary>
        /// The corpse's holder (OmniCell HolderType/HolderInstance) - not the dead dynel; that is
        /// <see cref="UnknownIdentity"/>.
        /// </summary>
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

        /// <summary>
        /// InventoryId (high byte, stat 55) and BodyLocation (low byte, stat 220) as one short (OmniCell
        /// InventoryIdAndBodyLocation).
        /// </summary>
        [AoMember(7)]
        public short Unknown3 { get; set; }

        /// <summary>
        /// The corpse's stats: stat 0 (Flags) first, then CorpseInstance (the dead mob), Cash, TimeExist, ...
        /// </summary>
        [AoMember(8, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<Stat, int>[] Stats { get; set; }

        /// <summary>
        /// "Remains of ..." - Int32-counted, the count includes the terminator.
        /// </summary>
        [AoMember(9, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        /// <summary>
        /// The lockable-item version (OmniCell LockableVersion).
        /// </summary>
        [AoMember(10)]
        public int Unknown4 { get; set; }

        /// <summary>
        /// Stat 299, lockdifficulty (OmniCell LockDifficulty).
        /// </summary>
        [AoMember(11)]
        public int Unknown5 { get; set; }

        /// <summary>
        /// Who may lock and unlock the corpse - the LockableItem_t keyholder list. Empty in every capture.
        /// </summary>
        [AoMember(12, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] Keyholders { get; set; }

        /// <summary>
        /// The chest-family version (OmniCell ChestVersion).
        /// </summary>
        [AoMember(13)]
        public int Unknown6 { get; set; }

        /// <summary>
        /// The effects on the corpse, read by the same client function as SpellList's (Gamecode.dll 0x100A71AE).
        /// Every captured corpse carries one, game function 53031 (its appearance).
        /// </summary>
        [AoMember(14)]
        public NanoEffectRecord[] NanoEffects { get; set; }

        /// <summary>
        /// The dynel that died (OmniCell Owner). The SDK marks this dynel dead (Client.cs).
        /// </summary>
        [AoMember(15)]
        public Identity UnknownIdentity { get; set; }

        /// <summary>
        /// Five entries in every capture, places 0 to 4.
        /// </summary>
        [AoMember(16, SerializeSize = ArraySizeType.X3F1)]
        public Texture[] Textures { get; set; }

        /// <summary>
        /// Whether <see cref="Meshes"/> follows at all: 0 means the message ends here with no array header;
        /// 1 precedes both one mesh and two, so it is a flag, not a count.
        /// </summary>
        [AoMember(17)]
        [AoFlags("corpseHasMeshes")]
        public int HasMeshes { get; set; }

        /// <summary>
        /// Texture overrides for the corpse's body parts (cosmetic), present only when <see cref="HasMeshes"/>
        /// is non-zero; empty (never null) otherwise.
        /// </summary>
        [AoMember(18, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("corpseHasMeshes", typeof(CorpseMesh[]), FlagsCriteria.HasAny, int.MaxValue)]
        public CorpseMesh[] Meshes { get => meshes; set => meshes = value ?? new CorpseMesh[0]; }

        private CorpseMesh[] meshes;

        #endregion

        #region Compatibility aliases

        /// <summary>
        /// Alias of <see cref="Unknown1"/> under OmniCell's name.
        /// </summary>
        public int MsgVersion { get => Unknown1; set => Unknown1 = value; }

        /// <summary>
        /// Alias of <see cref="Unknown5"/> under OmniCell's name.
        /// </summary>
        public int LockDifficulty { get => Unknown5; set => Unknown5 = value; }

        /// <summary>
        /// Alias of <see cref="UnknownIdentity"/>: the dynel this corpse was.
        /// </summary>
        public Identity Deceased { get => UnknownIdentity; set => UnknownIdentity = value; }

        /// <summary>
        /// The old int32 view of the keyholder list (type, instance, type, instance ...), kept for compatibility.
        /// Read-only; set <see cref="Keyholders"/>.
        /// </summary>
        public int[] UnknownArray => (Keyholders ?? new Identity[0]).SelectMany(k => new[] { (int)k.Type, k.Instance }).ToArray();

        /// <summary>
        /// The old fixed 15-int view of <see cref="NanoEffects"/>, kept for compatibility. Read-only; exact only for
        /// an effect with no criteria and seven int arguments (function 53031, the one every capture carries).
        /// </summary>
        public AnimationEffect[] AnimationEffects =>
            (NanoEffects ?? new NanoEffectRecord[0]).Select(e =>
            {
                var a = e.ArgumentInts;
                int Arg(int i) => i < a.Length ? a[i] : 0;
                return new AnimationEffect
                {
                    IdentityType = (int)e.Effect.Type,
                    NanoId = e.Effect.Instance,
                    NanoInstance = e.Version,
                    Time1 = e.CriterionCount,
                    Time2 = e.Hits,
                    Unknown1 = e.Amount,
                    Unknown2 = e.Target,
                    Unknown3 = e.SpellList,
                    Unknown4 = Arg(0),
                    Unknown5 = Arg(1),
                    Unknown6 = Arg(2),
                    Unknown7 = Arg(3),
                    Unknown8 = Arg(4),
                    VisualDataId = Arg(5),
                    Unknown9 = Arg(6)
                };
            }).ToArray();

        #endregion

        /// <summary>
        /// One corpse texture override (OmniCell GameData\CorpseMesh.cs): a fixed 32-byte name, the texture id,
        /// an overlay texture (layer 3, no alpha) and the blend mode. 44 bytes.
        /// </summary>
        public class CorpseMesh
        {
            /// <summary>
            /// Fixed width, not length prefixed ("Material #1", "arms", "head", ...).
            /// </summary>
            [AoMember(0, FixedSizeLength = 32)]
            public string Name { get; set; }

            /// <summary>
            /// The texture id.
            /// </summary>
            [AoMember(1)]
            public int Id { get; set; }

            /// <summary>
            /// A second texture drawn at layer 3; 0 in every capture.
            /// </summary>
            [AoMember(2)]
            public int OverlayId { get; set; }

            /// <summary>
            /// How <see cref="Id"/> is blended; 0 in every capture (5 is the only other value the client holds).
            /// </summary>
            [AoMember(3)]
            public int AlphaMode { get; set; }
        }
    }
}
