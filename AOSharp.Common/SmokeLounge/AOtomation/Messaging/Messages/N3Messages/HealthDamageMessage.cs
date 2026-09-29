// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HealthDamageMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the HealthDamageMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Layout and names ported from OmniCell's AOtomation.Messaging (verified against every recorded packet).
    // The old AOSharp names (TargetHp, Amount, Stat, Unk1, Target, Unk2) are kept below as aliases.
    [AoContract((int)N3MessageType.HealthDamage)]
    public class HealthDamageMessage : N3Message
    {
        #region Constructors and Destructors

        public HealthDamageMessage()
        {
            this.N3MessageType = N3MessageType.HealthDamage;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Stat 27, health - what the character has left after the change.
        /// </summary>
        /// <remarks>
        /// The dispatcher at 0x100A075E writes it straight into stat 0x1B.
        /// </remarks>
        [AoMember(0)]
        public int Health { get; set; }

        /// <summary>
        /// How much health moved, signed: negative is damage and anything else
        /// is a heal.
        /// </summary>
        /// <remarks>
        /// The dispatcher branches on the sign at 0x100A0664 and then displays
        /// the magnitude - it takes the absolute value with the usual
        /// cdq/xor/sub before handing it to the feedback line - so the sign
        /// chooses the line and the size fills it in.
        /// </remarks>
        [AoMember(1)]
        public int Delta { get; set; }

        /// <summary>
        /// What kind of damage it was.
        /// </summary>
        /// <remarks>
        /// The client's own word for this field: the formatter it ends up in
        /// looks the value up in a damage type table at Gamecode.dll 0x10036CF5
        /// and prints "Missing damagetype: %d" when it is not there. The table
        /// is built in one run at 0x10033C41 and DamageType is the whole of it.
        ///
        /// Zero is not a value in that table; the formatter substitutes 27,
        /// Unknown, at 0x10012C87 before looking anything up. The damage arm
        /// also tests for 474, Fall, before anything else, because falling has
        /// a message of its own.
        ///
        /// It is NOT the stat that moved: the client always writes this message
        /// into health (stat 0x1B). The bot recordings carry 0, 90, 91, 92, 94,
        /// 95 and 96 here, and never CurrentNano (214).
        /// </remarks>
        [AoMember(2)]
        public DamageType DamageType { get; set; }

        /// <summary>
        /// What killed the character, or None.
        /// </summary>
        /// <remarks>
        /// Non-zero sends the dispatcher into 0x1005B3D8, which switches on it
        /// to pick one of five Feedback_DeathBy... lines and then sets health
        /// to zero. In the bot recordings 22 copies carry 5 (SpellDamage), each
        /// with Health 0.
        /// </remarks>
        [AoMember(3)]
        public DeathCause DeathCause { get; set; }

        /// <summary>
        /// Who did it - the attacker, or the healer.
        /// </summary>
        /// <remarks>
        /// Not the target: the message's own Identity is the character whose
        /// health changed, and the dispatcher refuses to do anything unless it
        /// is a CanbeAffected. This one is resolved separately at 0x100A064E
        /// and handed to the feedback line as the other party. For a character
        /// healing itself the two are the same.
        /// </remarks>
        [AoMember(4)]
        public Identity Source { get; set; }

        /// <summary>
        /// The item that did it, as an instance. Zero when there is none.
        /// </summary>
        /// <remarks>
        /// It is half an identity and the client supplies the other half. The
        /// formatter tests it against zero at 0x10012C97 and, when it is set,
        /// builds an Identity with the type hard-coded to 1000020 and this as
        /// the instance, then hands it to the item manager's find-or-create at
        /// 0x10082EE2. What that builds is a DummyItemBase_t, and the formatter
        /// calls vtable slot 0x34 on it - a name getter that falls back to stat
        /// 446, nametemplate, when the object has no name of its own - and puts
        /// the answer in the message.
        ///
        /// So this names the weapon or the nano the damage came from.
        /// </remarks>
        [AoMember(5)]
        public int SourceItem { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        /// <summary>Old name for <see cref="Health"/> (the receiver's HP after the change).</summary>
        [Obsolete("Wire field is Health (stat 27 after the change).")]
        public int TargetHp { get => this.Health; set => this.Health = value; }

        /// <summary>Old name for <see cref="Delta"/> (signed: negative = damage).</summary>
        [Obsolete("Wire field is Delta (signed health change).")]
        public int Amount { get => this.Delta; set => this.Delta = value; }

        /// <summary>
        /// Old name for <see cref="DamageType"/>. It was never a stat: the value is the damage type
        /// (0 or an AC stat id 90-97, 168 nano, 474 fall, 489 backstab), cast to Stat.
        /// </summary>
        [Obsolete("Wire field is DamageType, not a stat. The client always applies this message to Health.")]
        public Stat Stat { get => (Stat)(int)this.DamageType; set => this.DamageType = (DamageType)(int)value; }

        /// <summary>Old name for <see cref="DeathCause"/>.</summary>
        [Obsolete("Wire field is DeathCause.")]
        public int Unk1 { get => (int)this.DeathCause; set => this.DeathCause = (DeathCause)value; }

        /// <summary>Old name for <see cref="Source"/>: the attacker or healer, NOT the target.</summary>
        [Obsolete("Wire field is Source (the attacker or healer). The character hit is the message Identity.")]
        public Identity Target { get => this.Source; set => this.Source = value; }

        /// <summary>Old name for <see cref="SourceItem"/>.</summary>
        [Obsolete("Wire field is SourceItem (instance of the item/nano, type 1000020).")]
        public int Unk2 { get => this.SourceItem; set => this.SourceItem = value; }

        #endregion
    }
}
