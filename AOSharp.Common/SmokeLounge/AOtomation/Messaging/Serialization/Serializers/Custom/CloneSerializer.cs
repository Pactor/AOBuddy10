// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CloneSerializer.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CloneSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging
    // (Serialization\Serializers\Custom\CloneSerializer.cs). Changes for this library only: AOSharp's
    // Identity/Vector3/Quaternion types; the cloth record is ClothData (the client's ClothData_t), a copy of
    // OmniCell's Texture with its OverlayId/AlphaMode pair, because this library's Texture has only the first three
    // fields; Identities read and written as two int32s (this library's StreamReader/StreamWriter have no Identity
    // helpers).

    using AOSharp.Common.GameData;
    using System;
    using System.Linq.Expressions;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    /// <summary>
    /// Reads and writes Clone.
    /// </summary>
    /// <remarks>
    /// Written by hand for one reason: a ClothData entry is twelve bytes or
    /// twenty, and which it is depends on the value of its own first field. The
    /// attribute serializer has no way to say that, and would write the two
    /// optional ints on every entry.
    ///
    /// Transcribed from Gamecode.dll 0x10072E3B, and checked against the writer
    /// at 0x10072DDC, which emits the same six fields in the same order.
    /// </remarks>
    public class CloneSerializer : ISerializer
    {
        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public CloneSerializer()
        {
            this.type = typeof(CloneMessage);
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
            var clone = new CloneMessage();

            clone.N3MessageType = (N3MessageType)streamReader.ReadInt32();
            clone.Identity = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());
            clone.Unknown = streamReader.ReadByte();

            clone.Clothes = new ClothData[X3F1Count(streamReader.ReadInt32())];
            for (var i = 0; i < clone.Clothes.Length; i++)
            {
                var cloth = new ClothData
                            {
                                Place = streamReader.ReadInt32(),
                                Id = streamReader.ReadInt32(),
                                Group = streamReader.ReadInt32()
                            };

                if (cloth.HasExtra)
                {
                    cloth.OverlayId = streamReader.ReadInt32();
                    cloth.AlphaMode = streamReader.ReadInt32();
                }

                clone.Clothes[i] = cloth;
            }

            clone.HeadMesh = streamReader.ReadInt32();
            clone.Race = streamReader.ReadByte();
            clone.Breed = streamReader.ReadByte();
            clone.Sex = streamReader.ReadByte();
            clone.Name = ReadNullTerminated(streamReader);

            return clone;
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var clone = (CloneMessage)value;

            streamWriter.WriteInt32((int)clone.N3MessageType);
            streamWriter.WriteInt32((int)clone.Identity.Type);
            streamWriter.WriteInt32(clone.Identity.Instance);
            streamWriter.WriteByte(clone.Unknown);

            var clothes = clone.Clothes ?? new ClothData[0];
            streamWriter.WriteInt32((clothes.Length + 1) * 0x3F1);
            foreach (var cloth in clothes)
            {
                streamWriter.WriteInt32(cloth.Place);
                streamWriter.WriteInt32(cloth.Id);
                streamWriter.WriteInt32(cloth.Group);

                if (cloth.HasExtra)
                {
                    streamWriter.WriteInt32(cloth.OverlayId.GetValueOrDefault());
                    streamWriter.WriteInt32(cloth.AlphaMode.GetValueOrDefault());
                }
            }

            streamWriter.WriteInt32(clone.HeadMesh);
            streamWriter.WriteByte(clone.Race);
            streamWriter.WriteByte(clone.Breed);
            streamWriter.WriteByte(clone.Sex);

            string name = clone.Name ?? string.Empty;
            streamWriter.WriteString(name, name.Length);
            streamWriter.WriteByte(0);
        }

        #endregion

        #region Methods

        /// <summary>
        /// A string with no count in front of it, ending at the first zero.
        /// </summary>
        private static string ReadNullTerminated(StreamReader streamReader)
        {
            var text = new System.Text.StringBuilder();
            for (;;)
            {
                var b = streamReader.ReadByte();
                if (b == 0)
                {
                    return text.ToString();
                }

                text.Append((char)b);
            }
        }

        /// <summary>
        /// Turns an X3F1 encoded array header into an element count.
        /// </summary>
        private static int X3F1Count(int value)
        {
            if (value <= 0 || value % 0x3F1 != 0 || (value / 0x3F1) - 1 > 0x7530)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "{0} is not an X3F1 array header, so this read is already at the wrong offset",
                        value));
            }

            return (value / 0x3F1) - 1;
        }

        #endregion

        #region Expression plumbing

        public Expression DeserializerExpression(
            ParameterExpression streamReaderExpression,
            ParameterExpression serializationContextExpression,
            Expression assignmentTargetExpression,
            PropertyMetaData propertyMetaData)
        {
            var deserializerMethodInfo =
                ReflectionHelper
                    .GetMethodInfo
                    <CloneSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>(
                        o => o.Deserialize);
            var callExp = Expression.Call(
                Expression.New(this.GetType()),
                deserializerMethodInfo,
                new Expression[]
                    {
                        streamReaderExpression, serializationContextExpression,
                        Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                    });

            return Expression.Assign(
                assignmentTargetExpression, Expression.TypeAs(callExp, assignmentTargetExpression.Type));
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
                    <CloneSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(
                        o => o.Serialize);
            return Expression.Call(
                Expression.New(this.GetType()),
                serializerMethodInfo,
                new[]
                    {
                        streamWriterExpression, serializationContextExpression, valueExpression,
                        Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                    });
        }

        #endregion
    }
}
