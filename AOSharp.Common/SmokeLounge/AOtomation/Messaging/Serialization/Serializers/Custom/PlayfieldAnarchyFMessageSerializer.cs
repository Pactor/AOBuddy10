// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldAnarchyFMessageSerializer.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PlayfieldAnarchyFMessageSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System;
    using System.Linq.Expressions;

    using AOSharp.Common.GameData;

    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using static SmokeLounge.AOtomation.Messaging.Messages.N3Messages.PlayfieldAnarchyFMessage;

    /// <summary>
    /// Reads and writes <see cref="PlayfieldAnarchyFMessage"/>.
    /// </summary>
    /// <remarks>
    /// Ported 2026-09-29 from OmniCell's Serialization\Serializers\Custom\PlayfieldAnarchyFSerializer.cs (named
    /// differently here so it cannot collide with a port of OmniCell's own class). Hand-written because the tail is
    /// a DbObject that reads itself: the client peeks an identity, and only when it is not empty reads a revision
    /// and an object whose layout depends on the identity type.
    ///
    /// One deliberate difference: OmniCell throws on an unknown generator type. Here the unknown block is kept raw
    /// (<see cref="PlayfieldAnarchyFMessage.UnknownGeneratorData"/>, everything up to the final two int32s) so a
    /// never-seen generator cannot drop the whole zone-in message.
    /// </remarks>
    public class PlayfieldAnarchyFMessageSerializer : ISerializer
    {
        private const int BuildingGenerator = 51103;

        private const int OwnedBuildingGenerator = 51067;

        private const int TemplateGenerator = 51069;

        public Type Type => typeof(PlayfieldAnarchyFMessage);

        public object Deserialize(
            StreamReader streamReader,
            SerializationContext serializationContext,
            PropertyMetaData propertyMetaData = null)
        {
            var message = new PlayfieldAnarchyFMessage();

            message.N3MessageType = (N3MessageType)streamReader.ReadInt32();
            message.Identity = ReadIdentity(streamReader);
            message.Unknown = streamReader.ReadByte();

            message.Version = streamReader.ReadInt32();
            message.CharacterCoordinates = ReadVector3(streamReader);

            if (message.Version > 1)
            {
                message.TokenMarker = streamReader.ReadByte();
                message.ModelId = ReadIdentity(streamReader);
                message.Group = streamReader.ReadInt32();
                message.Subgroup = streamReader.ReadInt32();
                message.PlayfieldId = ReadIdentity(streamReader);
            }

            if (message.Version > 3)
            {
                Identity peeked = ReadIdentity(streamReader);
                message.GeneratorIdentity = peeked;
                if (peeked.Type != 0 || peeked.Instance != 0)
                {
                    int revision = streamReader.ReadInt32();
                    message.GeneratorRevision = revision;
                    switch ((int)peeked.Type)
                    {
                        case BuildingGenerator:
                            message.Generator = ReadBuildingGenerator(streamReader, peeked, revision);
                            break;
                        case TemplateGenerator:
                            message.TemplateGenerator = ReadTemplateGenerator(streamReader, peeked, revision);
                            break;
                        case OwnedBuildingGenerator:
                            message.OwnedBuildingGenerator = ReadOwnedBuildingGenerator(streamReader, peeked, revision);
                            break;
                        default:
                            // Never captured: keep the block raw, leaving the two trailing int32s to read below.
                            message.UnknownGeneratorData = streamReader.ReadBytes(Math.Max(0, streamReader.PeekUntilEnd() - 8));
                            break;
                    }
                }
            }

            message.PlayfieldX = streamReader.ReadInt32();
            message.PlayfieldZ = streamReader.ReadInt32();
            return message;
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var message = (PlayfieldAnarchyFMessage)value;

            streamWriter.WriteInt32((int)message.N3MessageType);
            WriteIdentity(streamWriter, message.Identity);
            streamWriter.WriteByte(message.Unknown);

            streamWriter.WriteInt32(message.Version);
            WriteVector3(streamWriter, message.CharacterCoordinates);

            if (message.Version > 1)
            {
                streamWriter.WriteByte(message.TokenMarker);
                WriteIdentity(streamWriter, message.ModelId);
                streamWriter.WriteInt32(message.Group);
                streamWriter.WriteInt32(message.Subgroup);
                WriteIdentity(streamWriter, message.PlayfieldId);
            }

            if (message.Version > 3)
            {
                if (message.Generator != null)
                {
                    WriteBuildingGenerator(streamWriter, message.Generator);
                }
                else if (message.TemplateGenerator != null)
                {
                    WriteTemplateGenerator(streamWriter, message.TemplateGenerator);
                }
                else if (message.OwnedBuildingGenerator != null)
                {
                    WriteOwnedBuildingGenerator(streamWriter, message.OwnedBuildingGenerator);
                }
                else if (message.UnknownGeneratorData != null)
                {
                    WriteIdentity(streamWriter, message.GeneratorIdentity);
                    streamWriter.WriteInt32(message.GeneratorRevision);
                    streamWriter.WriteBytes(message.UnknownGeneratorData);
                }
                else
                {
                    // An empty identity, which is where the client stops.
                    streamWriter.WriteInt32(0);
                    streamWriter.WriteInt32(0);
                }
            }

            streamWriter.WriteInt32(message.PlayfieldX);
            streamWriter.WriteInt32(message.PlayfieldZ);
        }

        public Expression DeserializerExpression(
            ParameterExpression streamReaderExpression,
            ParameterExpression serializationContextExpression,
            Expression assignmentTargetExpression,
            PropertyMetaData propertyMetaData)
        {
            var method = ReflectionHelper.GetMethodInfo<PlayfieldAnarchyFMessageSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>(o => o.Deserialize);
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
            var method = ReflectionHelper.GetMethodInfo<PlayfieldAnarchyFMessageSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(o => o.Serialize);
            return Expression.Call(
                Expression.Constant(this),
                method,
                streamWriterExpression,
                serializationContextExpression,
                Expression.Convert(valueExpression, typeof(object)),
                Expression.Constant(propertyMetaData, typeof(PropertyMetaData)));
        }

        private static BuildingGeneratorData ReadBuildingGenerator(StreamReader streamReader, Identity identity, int revision)
        {
            var data = new BuildingGeneratorData { Identity = identity, Revision = revision };
            data.Version = streamReader.ReadInt16();
            data.Width = streamReader.ReadInt16();
            data.Height = streamReader.ReadInt16();
            data.WorldHeight = streamReader.ReadInt16();
            data.TemplatePlayfield = streamReader.ReadInt32();
            data.AmbientRed = streamReader.ReadByte();
            data.AmbientGreen = streamReader.ReadByte();
            data.AmbientBlue = streamReader.ReadByte();

            var rooms = new BuildingRoomInfo[Count(streamReader.ReadInt32())];
            for (var i = 0; i < rooms.Length; i++)
            {
                rooms[i] = new BuildingRoomInfo
                           {
                               Room = streamReader.ReadInt16(),
                               Floor = (sbyte)streamReader.ReadByte(),
                               X = streamReader.ReadByte(),
                               Z = streamReader.ReadByte(),
                               Rotation = streamReader.ReadByte()
                           };
            }

            data.Rooms = rooms;
            return data;
        }

        private static void WriteBuildingGenerator(StreamWriter streamWriter, BuildingGeneratorData data)
        {
            WriteIdentity(streamWriter, data.Identity);
            streamWriter.WriteInt32(data.Revision);
            streamWriter.WriteInt16(data.Version);
            streamWriter.WriteInt16(data.Width);
            streamWriter.WriteInt16(data.Height);
            streamWriter.WriteInt16(data.WorldHeight);
            streamWriter.WriteInt32(data.TemplatePlayfield);
            streamWriter.WriteByte(data.AmbientRed);
            streamWriter.WriteByte(data.AmbientGreen);
            streamWriter.WriteByte(data.AmbientBlue);

            var rooms = data.Rooms ?? new BuildingRoomInfo[0];
            streamWriter.WriteInt32(rooms.Length);
            foreach (var room in rooms)
            {
                streamWriter.WriteInt16(room.Room);
                streamWriter.WriteByte((byte)room.Floor);
                streamWriter.WriteByte(room.X);
                streamWriter.WriteByte(room.Z);
                streamWriter.WriteByte(room.Rotation);
            }
        }

        private static PlayfieldTemplateGeneratorData ReadTemplateGenerator(StreamReader streamReader, Identity identity, int revision)
        {
            var data = new PlayfieldTemplateGeneratorData { Identity = identity, Revision = revision };
            data.Version = streamReader.ReadInt32();

            var runs = new PlayfieldDynelRun[Count(streamReader.ReadInt32())];
            for (var i = 0; i < runs.Length; i++)
            {
                runs[i] = new PlayfieldDynelRun
                          {
                              Type = (IdentityType)streamReader.ReadInt32(),
                              Unknown = streamReader.ReadInt32(),
                              StartIndex = streamReader.ReadInt32(),
                              Count = streamReader.ReadInt32(),
                              FirstInstance = streamReader.ReadInt32()
                          };
            }

            data.Runs = runs;
            return data;
        }

        private static void WriteTemplateGenerator(StreamWriter streamWriter, PlayfieldTemplateGeneratorData data)
        {
            WriteIdentity(streamWriter, data.Identity);
            streamWriter.WriteInt32(data.Revision);
            streamWriter.WriteInt32(data.Version);

            var runs = data.Runs ?? new PlayfieldDynelRun[0];
            streamWriter.WriteInt32(runs.Length);
            foreach (var run in runs)
            {
                streamWriter.WriteInt32((int)run.Type);
                streamWriter.WriteInt32(run.Unknown);
                streamWriter.WriteInt32(run.StartIndex);
                streamWriter.WriteInt32(run.Count);
                streamWriter.WriteInt32(run.FirstInstance);
            }
        }

        private static OwnedBuildingGeneratorData ReadOwnedBuildingGenerator(StreamReader streamReader, Identity identity, int revision)
        {
            var data = new OwnedBuildingGeneratorData { Identity = identity, Revision = revision };
            data.Version = streamReader.ReadInt32();
            data.Unknown1 = streamReader.ReadInt32();
            data.Model = ReadIdentity(streamReader);
            data.EntranceDoor = streamReader.ReadInt32();
            data.Position = ReadVector3(streamReader);
            data.Marker = streamReader.ReadInt32();

            data.SecondListCount = streamReader.ReadInt32();
            if (data.SecondListCount != 0)
            {
                // Never captured, and its record length is unknown (OmniCell throws here). Keep the rest of the
                // generator raw so the zone-in itself still arrives; the two trailing int32s are read by the caller.
                data.UnreadTail = streamReader.ReadBytes(Math.Max(0, streamReader.PeekUntilEnd() - 8));
                data.Runs = new OwnedBuildingDynelRun[0];
                return data;
            }

            data.Unknown3 = streamReader.ReadInt32();
            data.Unknown4 = streamReader.ReadInt32();
            data.Unknown5 = streamReader.ReadInt32();

            var runs = new OwnedBuildingDynelRun[Count(streamReader.ReadInt32())];
            for (var i = 0; i < runs.Length; i++)
            {
                var run = new OwnedBuildingDynelRun { Type = (IdentityType)streamReader.ReadInt32() };
                var placements = new OwnedBuildingPlacement[Count(streamReader.ReadInt32())];
                for (var p = 0; p < placements.Length; p++)
                {
                    placements[p] = new OwnedBuildingPlacement
                                    {
                                        StartIndex = streamReader.ReadInt32(),
                                        Count = streamReader.ReadInt32(),
                                        FirstInstance = streamReader.ReadInt32()
                                    };
                }

                run.Placements = placements;
                runs[i] = run;
            }

            data.Runs = runs;
            return data;
        }

        private static void WriteOwnedBuildingGenerator(StreamWriter streamWriter, OwnedBuildingGeneratorData data)
        {
            WriteIdentity(streamWriter, data.Identity);
            streamWriter.WriteInt32(data.Revision);
            streamWriter.WriteInt32(data.Version);
            streamWriter.WriteInt32(data.Unknown1);
            WriteIdentity(streamWriter, data.Model);
            streamWriter.WriteInt32(data.EntranceDoor);
            WriteVector3(streamWriter, data.Position);
            streamWriter.WriteInt32(data.Marker);
            streamWriter.WriteInt32(data.SecondListCount);
            if (data.SecondListCount != 0)
            {
                streamWriter.WriteBytes(data.UnreadTail ?? new byte[0]);
                return;
            }

            streamWriter.WriteInt32(data.Unknown3);
            streamWriter.WriteInt32(data.Unknown4);
            streamWriter.WriteInt32(data.Unknown5);

            var runs = data.Runs ?? new OwnedBuildingDynelRun[0];
            streamWriter.WriteInt32(runs.Length);
            foreach (var run in runs)
            {
                streamWriter.WriteInt32((int)run.Type);
                var placements = run.Placements ?? new OwnedBuildingPlacement[0];
                streamWriter.WriteInt32(placements.Length);
                foreach (var placement in placements)
                {
                    streamWriter.WriteInt32(placement.StartIndex);
                    streamWriter.WriteInt32(placement.Count);
                    streamWriter.WriteInt32(placement.FirstInstance);
                }
            }
        }

        private static int Count(int value)
        {
            if (value < 0 || value > 100000)
            {
                throw new InvalidOperationException(string.Format("{0} is not a count, so this playfield message cannot be read", value));
            }

            return value;
        }

        private static Identity ReadIdentity(StreamReader streamReader)
        {
            return new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());
        }

        private static void WriteIdentity(StreamWriter streamWriter, Identity identity)
        {
            streamWriter.WriteInt32((int)identity.Type);
            streamWriter.WriteInt32(identity.Instance);
        }

        private static Vector3 ReadVector3(StreamReader streamReader)
        {
            return new Vector3(streamReader.ReadSingle(), streamReader.ReadSingle(), streamReader.ReadSingle());
        }

        private static void WriteVector3(StreamWriter streamWriter, Vector3 vector)
        {
            streamWriter.WriteSingle(vector.X);
            streamWriter.WriteSingle(vector.Y);
            streamWriter.WriteSingle(vector.Z);
        }
    }
}
