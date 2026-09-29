// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InfoPacketSerializer.cs" company="AOBuddy10">
//   Added 2026-09-29 to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the InfoPacketSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System;
    using System.Linq.Expressions;

    using AOSharp.Common.GameData;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    /// <summary>
    /// Reads and writes the <see cref="InfoPacket"/> record of an InfoPacketMessage for any flag combination.
    /// </summary>
    /// <remarks>
    /// Registered for the type <see cref="InfoPacket"/>. The flags byte is read first by InfoPacketMessage and
    /// published in the serialization context under <see cref="InfoPacketMessage.InfoPacketFlagsKey"/>; this reads
    /// it back, picks the subclass (<see cref="CharacterInfoPacket"/> when NotAPlayer is clear,
    /// <see cref="TowerInfoPacket"/> when NotAPlayer is set with HasAcgItems or Suppression, else
    /// <see cref="MonsterInfoPacket"/>) and walks the record in the client's order (Gamecode 0x10045F6C, as laid
    /// out in OmniCell's GameData\InfoPacket.cs). Hand-written because the block choice is by single bits and the
    /// version byte itself gates a pad byte, which the attribute serializer cannot express without nullable
    /// flag members.
    /// </remarks>
    public class InfoPacketSerializer : ISerializer
    {
        public Type Type => typeof(InfoPacket);

        public object Deserialize(
            StreamReader streamReader,
            SerializationContext serializationContext,
            PropertyMetaData propertyMetaData = null)
        {
            var flags = (InfoPacketFlags)serializationContext.GetFlagValue(InfoPacketMessage.InfoPacketFlagsKey);
            InfoPacket info = Create(flags);

            info.Version = (flags & InfoPacketFlags.Versioned) != 0 ? streamReader.ReadByte() : (byte)0;
            info.Profession = (Profession)streamReader.ReadByte();
            info.Level = streamReader.ReadByte();
            info.TitleLevel = streamReader.ReadByte();
            info.UnversionedPad = info.Version == 0 ? streamReader.ReadByte() : (byte?)null;
            info.VisualProfession = (Profession)streamReader.ReadByte();
            info.SideXp = streamReader.ReadInt16();
            info.Health = streamReader.ReadInt32();
            info.MaxHealth = streamReader.ReadInt32();
            info.BreedHostility = streamReader.ReadInt32();
            info.OrganizationId = streamReader.ReadInt32();
            info.FirstName = ReadString16(streamReader);
            info.LastName = ReadString16(streamReader);
            info.AuxiliaryName = ReadString16(streamReader);
            info.DisplayText = ReadString16(streamReader);

            if ((flags & InfoPacketFlags.Organization) != 0)
            {
                info.OrganizationRank = ReadString16(streamReader);
                if ((flags & InfoPacketFlags.OrganizationCities) != 0)
                {
                    info.TowerFields = new TowerField[X3F1Count(streamReader.ReadInt32())];
                    for (var i = 0; i < info.TowerFields.Length; i++)
                    {
                        info.TowerFields[i] = new TowerField
                                              {
                                                  Unknown1 = streamReader.ReadInt32(),
                                                  Identity = ReadIdentity(streamReader),
                                                  Name = ReadString16(streamReader),
                                                  Unknown2 = streamReader.ReadInt32(),
                                                  Unknown3 = streamReader.ReadInt32()
                                              };
                    }
                }

                info.CityPlayfieldId = streamReader.ReadInt32();
            }

            if ((flags & InfoPacketFlags.HasAcgItems) != 0)
            {
                info.Towers = new Tower[X3F1Count(streamReader.ReadInt32())];
                for (var i = 0; i < info.Towers.Length; i++)
                {
                    info.Towers[i] = new Tower
                                     {
                                         LowId = streamReader.ReadInt32(),
                                         HighId = streamReader.ReadInt32(),
                                         Quality = streamReader.ReadInt32(),
                                         Unknown = streamReader.ReadInt32()
                                     };
                }
            }

            if ((flags & InfoPacketFlags.Suppression) != 0)
            {
                info.SuppressionTimer = streamReader.ReadInt32();
                info.SuppressionLevel = streamReader.ReadByte();
            }

            if ((flags & InfoPacketFlags.HasFactionStandings) != 0)
            {
                info.FactionStandings = new int[12];
                for (var i = 0; i < 12; i++)
                {
                    info.FactionStandings[i] = streamReader.ReadInt32();
                }
            }

            info.InvadersKilled = streamReader.ReadInt32();
            info.KilledByInvaders = streamReader.ReadInt32();
            info.AiLevel = streamReader.ReadInt32();

            if ((flags & InfoPacketFlags.NotAPlayer) == 0)
            {
                info.PvpDuelWins = streamReader.ReadInt32();
                info.PvpDuelLoses = streamReader.ReadInt32();
                info.PvpProfessionDuelLoses = streamReader.ReadInt32();
                info.PvpSoloKills = streamReader.ReadInt32();
                info.PvpTeamKills = streamReader.ReadInt32();
                info.PvpSoloScore = streamReader.ReadInt32();
                info.PvpTeamScore = streamReader.ReadInt32();
                info.PvpDuelScore = streamReader.ReadInt32();
            }

            return info;
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var info = (InfoPacket)value;
            var flags = (InfoPacketFlags)serializationContext.GetFlagValue(InfoPacketMessage.InfoPacketFlagsKey);

            if ((flags & InfoPacketFlags.Versioned) != 0)
            {
                streamWriter.WriteByte(info.Version);
            }

            streamWriter.WriteByte((byte)info.Profession);
            streamWriter.WriteByte(info.Level);
            streamWriter.WriteByte(info.TitleLevel);
            if ((flags & InfoPacketFlags.Versioned) == 0 || info.Version == 0)
            {
                streamWriter.WriteByte(info.UnversionedPad ?? 0);
            }

            streamWriter.WriteByte((byte)info.VisualProfession);
            streamWriter.WriteInt16(info.SideXp);
            streamWriter.WriteInt32(info.Health);
            streamWriter.WriteInt32(info.MaxHealth);
            streamWriter.WriteInt32(info.BreedHostility);
            streamWriter.WriteInt32(info.OrganizationId);
            WriteString16(streamWriter, info.FirstName);
            WriteString16(streamWriter, info.LastName);
            WriteString16(streamWriter, info.AuxiliaryName);
            WriteString16(streamWriter, info.DisplayText);

            if ((flags & InfoPacketFlags.Organization) != 0)
            {
                WriteString16(streamWriter, info.OrganizationRank);
                if ((flags & InfoPacketFlags.OrganizationCities) != 0)
                {
                    var fields = info.TowerFields ?? new TowerField[0];
                    streamWriter.WriteInt32((fields.Length + 1) * 0x3F1);
                    foreach (var field in fields)
                    {
                        streamWriter.WriteInt32(field.Unknown1);
                        streamWriter.WriteInt32((int)field.Identity.Type);
                        streamWriter.WriteInt32(field.Identity.Instance);
                        WriteString16(streamWriter, field.Name);
                        streamWriter.WriteInt32(field.Unknown2);
                        streamWriter.WriteInt32(field.Unknown3);
                    }
                }

                streamWriter.WriteInt32(info.CityPlayfieldId ?? 0);
            }

            if ((flags & InfoPacketFlags.HasAcgItems) != 0)
            {
                var items = info.Towers ?? new Tower[0];
                streamWriter.WriteInt32((items.Length + 1) * 0x3F1);
                foreach (var item in items)
                {
                    streamWriter.WriteInt32(item.LowId);
                    streamWriter.WriteInt32(item.HighId);
                    streamWriter.WriteInt32(item.Quality);
                    streamWriter.WriteInt32(item.Unknown);
                }
            }

            if ((flags & InfoPacketFlags.Suppression) != 0)
            {
                streamWriter.WriteInt32(info.SuppressionTimer ?? 0);
                streamWriter.WriteByte(info.SuppressionLevel ?? 0);
            }

            if ((flags & InfoPacketFlags.HasFactionStandings) != 0)
            {
                for (var i = 0; i < 12; i++)
                {
                    streamWriter.WriteInt32(info.FactionStandings != null && i < info.FactionStandings.Length ? info.FactionStandings[i] : 0);
                }
            }

            streamWriter.WriteInt32(info.InvadersKilled);
            streamWriter.WriteInt32(info.KilledByInvaders);
            streamWriter.WriteInt32(info.AiLevel);

            if ((flags & InfoPacketFlags.NotAPlayer) == 0)
            {
                streamWriter.WriteInt32(info.PvpDuelWins);
                streamWriter.WriteInt32(info.PvpDuelLoses);
                streamWriter.WriteInt32(info.PvpProfessionDuelLoses);
                streamWriter.WriteInt32(info.PvpSoloKills);
                streamWriter.WriteInt32(info.PvpTeamKills);
                streamWriter.WriteInt32(info.PvpSoloScore);
                streamWriter.WriteInt32(info.PvpTeamScore);
                streamWriter.WriteInt32(info.PvpDuelScore);
            }
        }

        public Expression DeserializerExpression(
            ParameterExpression streamReaderExpression,
            ParameterExpression serializationContextExpression,
            Expression assignmentTargetExpression,
            PropertyMetaData propertyMetaData)
        {
            var method = ReflectionHelper.GetMethodInfo<InfoPacketSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>(o => o.Deserialize);
            var call = Expression.Call(
                Expression.Constant(this),
                method,
                streamReaderExpression,
                serializationContextExpression,
                Expression.Constant(propertyMetaData, typeof(PropertyMetaData)));
            return Expression.Assign(assignmentTargetExpression, Expression.TypeAs(call, assignmentTargetExpression.Type));
        }

        public Expression SerializerExpression(
            ParameterExpression streamWriterExpression,
            ParameterExpression serializationContextExpression,
            Expression valueExpression,
            PropertyMetaData propertyMetaData)
        {
            var method = ReflectionHelper.GetMethodInfo<InfoPacketSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(o => o.Serialize);
            return Expression.Call(
                Expression.Constant(this),
                method,
                streamWriterExpression,
                serializationContextExpression,
                Expression.Convert(valueExpression, typeof(object)),
                Expression.Constant(propertyMetaData, typeof(PropertyMetaData)));
        }

        private static InfoPacket Create(InfoPacketFlags flags)
        {
            if ((flags & InfoPacketFlags.NotAPlayer) == 0)
            {
                return new CharacterInfoPacket();
            }

            if ((flags & (InfoPacketFlags.HasAcgItems | InfoPacketFlags.Suppression)) != 0)
            {
                return new TowerInfoPacket();
            }

            return new MonsterInfoPacket();
        }

        private static string ReadString16(StreamReader streamReader)
        {
            return streamReader.ReadString(streamReader.ReadInt16());
        }

        private static void WriteString16(StreamWriter streamWriter, string value)
        {
            value = value ?? string.Empty;
            streamWriter.WriteInt16((short)value.Length);
            streamWriter.WriteString(value);
        }

        private static Identity ReadIdentity(StreamReader streamReader)
        {
            return new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());
        }

        private static int X3F1Count(int value)
        {
            if (value <= 0 || value % 0x3F1 != 0 || (value / 0x3F1) - 1 > 0x7530)
            {
                throw new InvalidOperationException(string.Format("{0} is not an X3F1 count, so this list cannot be read", value));
            }

            return (value / 0x3F1) - 1;
        }
    }
}
