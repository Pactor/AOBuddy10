// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleCharFullUpdateSerializer.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SimpleCharFullUpdateSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    // TODO: Check the client side of this message for the possibly missing parts.
    public class SimpleCharFullUpdateSerializer : ISerializer
    {
        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public SimpleCharFullUpdateSerializer()
        {
            this.type = typeof(SimpleCharFullUpdateMessage);
        }

        #endregion

        #region Public Properties

        public Type Type
        {
            get
            {
                return this.type;
            }
        }

        #endregion

        #region Public Methods and Operators

        public object Deserialize(
            StreamReader streamReader, 
            SerializationContext serializationContext, 
            PropertyMetaData propertyMetaData = null)
        {
            SimpleCharFullUpdateMessage scfu = new SimpleCharFullUpdateMessage();
            scfu.N3MessageType = (N3MessageType)streamReader.ReadInt32();
            scfu.Identity = new Identity
            {
                Type = (IdentityType)streamReader.ReadInt32(),
                Instance = streamReader.ReadInt32()
            };
            scfu.Unknown = streamReader.ReadByte();
            scfu.Version = streamReader.ReadByte();
            scfu.Flags = (SimpleCharFullUpdateFlags)streamReader.ReadInt32();

            if (scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasPlayfieldId))
                scfu.PlayfieldId = streamReader.ReadInt32();

            scfu.Position = new Vector3(streamReader.ReadSingle(), streamReader.ReadSingle(), streamReader.ReadSingle());
            scfu.Heading = scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasHeading) ? new Quaternion(streamReader.ReadSingle(), streamReader.ReadSingle(), streamReader.ReadSingle(), streamReader.ReadSingle()) : Quaternion.Identity;

            scfu.Appearance = new Appearance() { Value = streamReader.ReadUInt32() };
            scfu.Name = streamReader.ReadString(streamReader.ReadByte());
            scfu.CharacterFlags = (CharacterFlags)streamReader.ReadInt32();
            scfu.AccountFlags = streamReader.ReadInt16();
            scfu.Expansions = streamReader.ReadInt16();

            if (scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.IsNpc))
            {
                scfu.CharacterInfo = new SimpleCharInfo.NPCInfo();
                var snpc = scfu.CharacterInfo as SimpleCharInfo.NPCInfo;

                snpc.Family = scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasSmallNpcFamily) ? streamReader.ReadByte() : streamReader.ReadInt16();
                snpc.LosHeight = scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasSmallNpcLosHeight) ? streamReader.ReadByte() : streamReader.ReadInt16();

                if (scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.UnknownDataFlag))
                {
                    streamReader.ReadByte();
                    streamReader.ReadInt16();
                }
            }
            else
            {
                scfu.CharacterInfo = new SimpleCharInfo.PlayerInfo();
                var spc = scfu.CharacterInfo as SimpleCharInfo.PlayerInfo;

                spc.CurrentNano = streamReader.ReadUInt32();
                spc.Team = streamReader.ReadInt32();
                spc.Swim = streamReader.ReadInt16();
                spc.StrengthBase = streamReader.ReadInt16();
                spc.AgilityBase = streamReader.ReadInt16();
                spc.StaminaBase = streamReader.ReadInt16();
                spc.IntelligenceBase = streamReader.ReadInt16();
                spc.SenseBase = streamReader.ReadInt16();
                spc.PsychicBase = streamReader.ReadInt16();

                if (scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasOrgName))
                {
                    spc.OrgId = streamReader.ReadInt32();
                    streamReader.ReadByte();
                }

                if (scfu.CharacterFlags.HasFlag(CharacterFlags.HasVisibleName))
                {
                    spc.FirstName = streamReader.ReadString(streamReader.ReadByte());
                    spc.LastName = streamReader.ReadString(streamReader.ReadByte());
                }

                if (scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasOrgName))
                    spc.OrgName = streamReader.ReadString(streamReader.ReadByte());
            }

            if (scfu.CharacterFlags.HasFlag(CharacterFlags.Tower))
            {
                scfu.ScfuTowerUnk = streamReader.ReadByte();
            }

            scfu.Level = scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasExtendedLevel) ? streamReader.ReadInt16() : streamReader.ReadByte();
            scfu.Health = scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasSmallHealth) ? streamReader.ReadUInt16() : streamReader.ReadInt32();
            scfu.HealthDamage = scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasSmallHealthDamage) ? streamReader.ReadByte() : scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasSmallHealth) ? streamReader.ReadUInt16() : streamReader.ReadInt32();

            scfu.MonsterData = streamReader.ReadUInt32();
            scfu.MonsterScale = streamReader.ReadInt16();
            scfu.VisualFlags = streamReader.ReadInt16();
            scfu.VisibleTitle = streamReader.ReadByte();

            scfu.ScfuUnk1 = streamReader.ReadBytes(streamReader.ReadInt32());

            if (scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasHeadMesh))
                scfu.HeadMesh = streamReader.ReadInt32();

            scfu.RunSpeedBase = scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasExtendedRunSpeed) ? streamReader.ReadInt16() : streamReader.ReadByte();

            if (scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.IsUnderAttack))
                scfu.FightingTarget = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());

            if (scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasExtendedTextures))
            {
                var len = streamReader.ReadInt32() / 1009 - 1;
                scfu.TextureOverrides = new SimpleCharInfo.TextureOverride[len];
                for (var i = 0; i < len; i++)
                {
                    var item = new SimpleCharInfo.TextureOverride();
                    item.Name = System.Text.Encoding.ASCII.GetString(streamReader.ReadBytes(32));
                    item.TextureId = streamReader.ReadInt32();
                    item.Unknown1 = streamReader.ReadInt32();
                    item.Unknown2 = streamReader.ReadInt32();
                    scfu.TextureOverrides[i] = item;
                }
            }

            if (scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.IsImmune))
            {
                var unk3Byte = streamReader.ReadByte();
            }

            if (scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.UnknownFlag3))
            {
                var unk3Byte = streamReader.ReadByte();
            }

            int activeNanoCount = (streamReader.ReadInt32() / 0x3F1) - 1;
            scfu.ActiveNanos = new SimpleCharInfo.ActiveNano[activeNanoCount];
            for (var i = 0; i < activeNanoCount; i++)
            {
                scfu.ActiveNanos[i] = new SimpleCharInfo.ActiveNano
                {
                    Identity = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32()),
                    NanoInstance = streamReader.ReadInt32(),
                    Time1 = streamReader.ReadInt32(),
                    Time2 = streamReader.ReadInt32()
                };
            }

            if (scfu.Flags.HasFlag(SimpleCharFullUpdateFlags.HasWaypoints))
            {
                streamReader.ReadUInt32();//Type
                streamReader.ReadUInt32();//Id

                scfu.Waypoints = new List<Vector3>();

                int waypointCount = streamReader.ReadInt32();
                for (var i = 0; i < waypointCount; i++)
                    scfu.Waypoints.Add(new Vector3(streamReader.ReadSingle(), streamReader.ReadSingle(), streamReader.ReadSingle()));
            }

            int texturesCount = (streamReader.ReadInt32() / 0x3F1) - 1;
            var texes = new List<Texture>();
            for (var i = 0; i < texturesCount; i++)
            {
                texes.Add(new Texture()
                {
                    Place = streamReader.ReadInt32(),
                    Id = streamReader.ReadInt32(),
                    Unknown = streamReader.ReadInt32()
                });
            }
            scfu.Textures = texes.ToArray();

            int meshesCount = (streamReader.ReadInt32() / 0x3F1) - 1;
            var meshes = new List<Mesh>();
            for (var i = 0; i < meshesCount; i++)
            {
                meshes.Add(new Mesh()
                {
                    Position = streamReader.ReadByte(),
                    Id = streamReader.ReadUInt32(),
                    OverrideTextureId = streamReader.ReadInt32(),
                    Layer = streamReader.ReadByte()
                });
            }
            scfu.Meshes = meshes.ToArray();

            scfu.Flags2 = (ScfuFlags2)streamReader.ReadInt32();

            if (scfu.Flags2.HasFlag(ScfuFlags2.HasOwner))
                scfu.Owner = new Identity(IdentityType.SimpleChar, streamReader.ReadInt32());

            scfu.ScfuUnk2 = streamReader.ReadByte();

            if (scfu.Flags2.HasFlag(ScfuFlags2.Unknown3))
            {
                var count = streamReader.ReadByte();
                scfu.SpecialAttacks = new SimpleCharInfo.SpecialAttackData[count];
                for (int i = 0; i < count; i++)
                {
                    short unk1 = streamReader.ReadInt16();

                    if (unk1 == 0)
                        continue;

                    scfu.SpecialAttacks[i] = new SimpleCharInfo.SpecialAttackData()
                    {
                        Unknown1 = unk1,
                        Unknown2 = streamReader.ReadInt16(),
                        Unknown3 = streamReader.ReadInt16(),
                        Unknown4 = streamReader.ReadInt16(),
                        Unknown5 = streamReader.ReadInt16(),
                        Name = streamReader.ReadString(4),
                        Unknown6 = streamReader.ReadInt16(),
                    };
                }
            }

            //Commenting these out until we need them.

            //if (scfu.Flags2.HasFlag(ScfuFlags2.Unknown1))
            //{
            //    scfu.ScfuUnk4 = streamReader.ReadByte();
            //}

            return scfu;
        }

        public Expression DeserializerExpression(
            ParameterExpression streamReaderExpression, 
            ParameterExpression serializationContextExpression, 
            Expression assignmentTargetExpression, 
            PropertyMetaData propertyMetaData)
        {
            var deserializerMethodInfo =
                ReflectionHelper
                    .GetMethodInfo
                    <SimpleCharFullUpdateSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>
                    (o => o.Deserialize);
            var serializerExp = Expression.New(this.GetType());
            var callExp = Expression.Call(
                serializerExp, 
                deserializerMethodInfo, 
                new Expression[]
                    {
                        streamReaderExpression, serializationContextExpression, 
                        Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                    });

            var assignmentExp = Expression.Assign(
                assignmentTargetExpression, Expression.TypeAs(callExp, assignmentTargetExpression.Type));
            return assignmentExp;
        }

        public void Serialize(
            StreamWriter streamWriter, 
            SerializationContext serializationContext, 
            object value, 
            PropertyMetaData propertyMetaData = null)
        {
            //Unused but leaving this here for reference if anyone needs.
            /*
            var scfu = (SimpleCharFullUpdateMessage)value;

            // N3Message
            streamWriter.WriteInt32((int)scfu.N3MessageType);
            streamWriter.WriteInt32((int)scfu.Identity.Type);
            streamWriter.WriteInt32(scfu.Identity.Instance);
            streamWriter.WriteByte(scfu.Unknown);

            // SCFU
            streamWriter.WriteByte(scfu.Version);
            streamWriter.WriteInt32((int)scfu.Flags); // Will update the flags later

            var flags = SimpleCharFullUpdateFlags.None;

            if (scfu.PlayfieldId.HasValue)
            {
                flags |= SimpleCharFullUpdateFlags.HasPlayfieldId;
                streamWriter.WriteInt32(scfu.PlayfieldId.Value);
            }

            if (scfu.FightingTarget != null)
            {
                flags |= SimpleCharFullUpdateFlags.HasFightingTarget;
                streamWriter.WriteInt32((int)scfu.Identity.Type);
                streamWriter.WriteInt32(scfu.Identity.Instance);
            }

            streamWriter.WriteSingle(scfu.Position.X);
            streamWriter.WriteSingle(scfu.Position.Y);
            streamWriter.WriteSingle(scfu.Position.Z);

            if (scfu.Heading.HasValue)
            {
                flags |= SimpleCharFullUpdateFlags.HasHeading;
                streamWriter.WriteSingle(scfu.Heading.Value.X);
                streamWriter.WriteSingle(scfu.Heading.Value.Y);
                streamWriter.WriteSingle(scfu.Heading.Value.Z);
                streamWriter.WriteSingle(scfu.Heading.Value.W);
            }

            streamWriter.WriteUInt32(scfu.Appearance.Value);

            streamWriter.WriteByte((byte)(scfu.Name.Length + 1));
            streamWriter.WriteString(scfu.Name, scfu.Name.Length + 1);

            streamWriter.WriteInt32((int)scfu.CharacterFlags);
            streamWriter.WriteInt16(scfu.AccountFlags);
            streamWriter.WriteInt16(scfu.Expansions);

            var snpc = scfu.CharacterInfo as SimpleCharInfo.NPCInfo;
            if (snpc != null)
            {
                flags |= SimpleCharFullUpdateFlags.IsNpc;
                if (snpc.Family > byte.MaxValue)
                {
                    flags |= SimpleCharFullUpdateFlags.HasSmallNpcFamily;
                    streamWriter.WriteByte((byte)snpc.Family);
                    streamWriter.WriteInt16(snpc.Family);
                }
                else
                {
                    streamWriter.WriteInt16(snpc.Family);
                }

                if (snpc.LosHeight > byte.MaxValue)
                {
                    flags |= SimpleCharFullUpdateFlags.HasSmallNpcLosHeight;
                    streamWriter.WriteByte((byte)snpc.LosHeight);
                }
                else
                {
                    streamWriter.WriteInt16(snpc.Family);
                }
            }

            var spc = scfu.CharacterInfo as SimpleCharInfo.PlayerInfo;
            if (spc != null)
            {
                streamWriter.WriteUInt32(spc.CurrentNano);
                streamWriter.WriteInt32(spc.Team);
                streamWriter.WriteInt16(spc.Swim);

                streamWriter.WriteInt16(spc.StrengthBase);
                streamWriter.WriteInt16(spc.AgilityBase);
                streamWriter.WriteInt16(spc.StaminaBase);
                streamWriter.WriteInt16(spc.IntelligenceBase);
                streamWriter.WriteInt16(spc.SenseBase);
                streamWriter.WriteInt16(spc.PsychicBase);

                if (scfu.CharacterFlags.HasFlag(CharacterFlags.HasVisibleName))
                {
                    streamWriter.WriteInt16((short)spc.FirstName.Length);
                    streamWriter.WriteString(spc.FirstName);
                    streamWriter.WriteInt16((short)spc.LastName.Length);
                    streamWriter.WriteString(spc.LastName);
                }

                if (string.IsNullOrWhiteSpace(spc.OrgName) == false)
                {
                    flags |= SimpleCharFullUpdateFlags.HasOrgName;
                    streamWriter.WriteInt16((short)spc.OrgName.Length);
                    streamWriter.WriteString(spc.OrgName);
                }
            }

            if (scfu.Level > sbyte.MaxValue)
            {
                flags |= SimpleCharFullUpdateFlags.HasExtendedLevel;
                streamWriter.WriteInt16(scfu.Level);
            }
            else
            {
                streamWriter.WriteByte((byte)scfu.Level);
            }

            if (scfu.Health <= short.MaxValue)
            {
                flags |= SimpleCharFullUpdateFlags.HasSmallHealth;
                streamWriter.WriteInt16((short)scfu.Health);
            }
            else
            {
                streamWriter.WriteUInt32(scfu.Health);
            }

            if (scfu.HealthDamage <= byte.MaxValue)
            {
                flags |= SimpleCharFullUpdateFlags.HasSmallHealthDamage;
                streamWriter.WriteByte((byte)scfu.HealthDamage);
            }
            else
            {
                if (flags.HasFlag(SimpleCharFullUpdateFlags.HasSmallHealth))
                {
                    streamWriter.WriteInt16((short)scfu.HealthDamage);
                }
                else
                {
                    streamWriter.WriteInt32(scfu.HealthDamage);
                }
            }

            streamWriter.WriteUInt32(scfu.MonsterData);
            streamWriter.WriteInt16(scfu.MonsterScale);
            streamWriter.WriteInt16(scfu.VisualFlags);
            streamWriter.WriteByte(scfu.VisibleTitle);

            streamWriter.WriteInt32(scfu.ScfuUnk1.Length);
            streamWriter.WriteBytes(scfu.ScfuUnk1);

            if (scfu.HeadMesh.HasValue)
            {
                flags |= SimpleCharFullUpdateFlags.HasHeadMesh;
                streamWriter.WriteUInt32(scfu.HeadMesh.Value);
            }

            if (scfu.RunSpeedBase > sbyte.MaxValue)
            {
                flags |= SimpleCharFullUpdateFlags.HasExtendedRunSpeed;
                streamWriter.WriteInt16(scfu.RunSpeedBase);
            }
            else
            {
                streamWriter.WriteByte((byte)scfu.RunSpeedBase);
            }

            streamWriter.WriteInt32((scfu.ActiveNanos.Length + 1) * 0x3F1);
            foreach (var activeNano in scfu.ActiveNanos)
            {
                streamWriter.WriteInt32((int)activeNano.Identity.Type);
                streamWriter.WriteInt32(activeNano.Identity.Instance);
                streamWriter.WriteInt32(activeNano.NanoInstance);
                streamWriter.WriteInt32(activeNano.Time1);
                streamWriter.WriteInt32(activeNano.Time2);
            }

            streamWriter.WriteInt32((scfu.Textures.Length + 1) * 0x3F1);
            foreach (var texture in scfu.Textures)
            {
                streamWriter.WriteInt32(texture.Place);
                streamWriter.WriteInt32(texture.Id);
                streamWriter.WriteInt32(texture.Unknown);
            }

            streamWriter.WriteInt32((scfu.Meshes.Length + 1) * 0x3F1);
            foreach (var mesh in scfu.Meshes)
            {
                streamWriter.WriteByte(mesh.Position);
                streamWriter.WriteUInt32(mesh.Id);
                streamWriter.WriteInt32(mesh.OverrideTextureId);
                streamWriter.WriteByte(mesh.Layer);
            }

            streamWriter.WriteInt32((int)scfu.Flags2);
            streamWriter.WriteByte(scfu.ScfuUnk2);

            var pos = streamWriter.Position;
            streamWriter.Position = 30;
            streamWriter.WriteInt32((int)flags);
            streamWriter.Position = pos;
            */
        }

        public Expression SerializerExpression(
            ParameterExpression streamWriterExpression, 
            ParameterExpression serializationContextExpression, 
            Expression valueExpression, 
            PropertyMetaData propertyMetaData)
        {
            var serializerMethodInfo =
                ReflectionHelper
                    .GetMethodInfo
                    <SimpleCharFullUpdateSerializer, 
                        Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(o => o.Serialize);
            var serializerExp = Expression.New(this.GetType());
            var callExp = Expression.Call(
                serializerExp, 
                serializerMethodInfo, 
                new[]
                    {
                        streamWriterExpression, serializationContextExpression, valueExpression, 
                        Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                    });
            return callExp;
        }

        #endregion
    }
}