// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NanoEffectRecord.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NanoEffectRecord type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using AOSharp.Common.GameData;

    /// <summary>
    /// One spell effect as the server sends it inside SpellList and CorpseFullUpdate (and FullCharacter's buff list).
    /// </summary>
    /// <remarks>
    /// Ported 2026-09-29 from OmniCell's GameData\NanoEffect.cs and Serialization\Serializers\Custom\NanoEffects.cs,
    /// which read every captured effect with zero bytes left over. The client reads it with one function
    /// (Gamecode.dll 0x100A71AE): the game function identity, a version, a counted list of criteria, four fixed
    /// ints, then the function's own arguments whose layout comes from the client's spell-format table
    /// (<see cref="Serialization.Serializers.Custom.NanoEffectRecordFormats"/>).
    ///
    /// Named NanoEffectRecord because this library's older <see cref="NanoEffect"/> is a different (wrong) fixed
    /// layout kept for compatibility. Read and written by
    /// <see cref="Serialization.Serializers.Custom.NanoEffectRecordListSerializer"/>, which is registered for
    /// <c>NanoEffectRecord[]</c>: any message member of that type is an X3F1-counted list of these.
    /// </remarks>
    public class NanoEffectRecord
    {
        /// <summary>
        /// The game function: Type is the function number (53002-53255, e.g. 53066 = the Ambient Restoration
        /// tick, 53031 = the corpse-appearance effect), Instance the effect's own id.
        /// </summary>
        public Identity Effect { get; set; }

        /// <summary>
        /// The effect record's version.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// How many criteria follow, as the wire carries it.
        /// </summary>
        public int CriterionCount { get; set; }

        /// <summary>
        /// The requirements that gate the effect (stat, value, operator), 12 bytes each.
        /// </summary>
        public NanoEffectCriterion[] Criteria { get; set; }

        /// <summary>
        /// How many times the effect fires (SpellStat 3).
        /// </summary>
        public int Hits { get; set; }

        /// <summary>
        /// The delay/amount word (SpellStat 4).
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// Who the effect lands on (SpellStat 32).
        /// </summary>
        public int Target { get; set; }

        /// <summary>
        /// The spell-list word (SpellStat 35).
        /// </summary>
        public int SpellList { get; set; }

        /// <summary>
        /// The function's own arguments, raw and in wire order: four bytes each, except a string argument, which
        /// is a four-byte length and that many bytes. <see cref="ArgumentInts"/> gives the four-byte ones.
        /// </summary>
        public byte[] Arguments { get; set; }

        /// <summary>
        /// <see cref="Arguments"/> read as consecutive big-endian int32s (valid when the function has no string
        /// argument, which is every function seen in SpellList/CorpseFullUpdate so far).
        /// </summary>
        public int[] ArgumentInts
        {
            get
            {
                var raw = this.Arguments ?? new byte[0];
                var ints = new int[raw.Length / 4];
                for (var i = 0; i < ints.Length; i++)
                {
                    ints[i] = (raw[i * 4] << 24) | (raw[i * 4 + 1] << 16) | (raw[i * 4 + 2] << 8) | raw[i * 4 + 3];
                }

                return ints;
            }
        }
    }

    /// <summary>
    /// One requirement on a <see cref="NanoEffectRecord"/>.
    /// </summary>
    public class NanoEffectCriterion
    {
        /// <summary>
        /// The stat tested.
        /// </summary>
        public Stat Stat { get; set; }

        /// <summary>
        /// The value it is tested against.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// The comparison (OmniCell NanoCriterionOperator: the client's requirement operator number).
        /// </summary>
        public int Operator { get; set; }
    }
}
