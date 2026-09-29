// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GfxTriggerSerializer.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the GfxTriggerSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging
    // (Serialization\Serializers\Custom\GfxTriggerSerializer.cs). Changes for this library only: AOSharp's
    // Identity/Vector3/Quaternion types; Identities read and written as two int32s (this library's
    // StreamReader/StreamWriter have no Identity helpers).

    using AOSharp.Common.GameData;
    using System;
    using System.Linq.Expressions;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    /// <summary>
    /// Six shapes, chosen by the first int32.
    /// </summary>
    /// <remarks>
    /// The client's reader at Gamecode 0x100398D3 and its writer at 0x10039991
    /// branch the same way on the same value, and the dispatcher at 0x10039A44
    /// hands each shape to a different overload of
    /// _EffectHandler_t::CreateEffect2. The attribute serializer cannot express
    /// a field that is present only when another field equals a particular
    /// value, which is why this exists.
    /// </remarks>
    public class GfxTriggerSerializer : ISerializer
    {
        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public GfxTriggerSerializer()
        {
            this.type = typeof(GfxTriggerMessage);
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
            var message = new GfxTriggerMessage();

            message.N3MessageType = (N3MessageType)streamReader.ReadInt32();
            message.Identity = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());
            message.Unknown = streamReader.ReadByte();

            message.Selector = streamReader.ReadInt32();
            message.EffectId = streamReader.ReadInt32();

            switch (message.Selector)
            {
                case 1:
                    message.Position = ReadVector3(streamReader);
                    break;

                case 2:
                    message.Target = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());
                    message.Attachment = (GfxAttachment)streamReader.ReadInt32();
                    break;

                case 3:
                    message.Position = ReadVector3(streamReader);
                    message.SecondPosition = ReadVector3(streamReader);
                    break;

                case 4:
                    message.Target = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());
                    message.Position = ReadVector3(streamReader);
                    break;

                case 5:
                    message.Target = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());
                    message.Position = ReadVector3(streamReader);
                    message.Attachment = (GfxAttachment)streamReader.ReadInt32();
                    break;

                case 6:
                    message.Target = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());
                    message.SecondTarget = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());
                    message.Attachment = (GfxAttachment)streamReader.ReadInt32();
                    break;
            }

            // Anything outside 1 to 6 carries no body at all: the client's
            // chain at 0x100398F4 falls through to the end without reading.
            return message;
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var message = (GfxTriggerMessage)value;

            streamWriter.WriteInt32((int)message.N3MessageType);
            streamWriter.WriteInt32((int)message.Identity.Type);
            streamWriter.WriteInt32(message.Identity.Instance);
            streamWriter.WriteByte(message.Unknown);

            streamWriter.WriteInt32(message.Selector);
            streamWriter.WriteInt32(message.EffectId);

            switch (message.Selector)
            {
                case 1:
                    WriteVector3(streamWriter, message.Position);
                    break;

                case 2:
                    streamWriter.WriteInt32((int)message.Target.Type); streamWriter.WriteInt32(message.Target.Instance);
                    streamWriter.WriteInt32((int)message.Attachment);
                    break;

                case 3:
                    WriteVector3(streamWriter, message.Position);
                    WriteVector3(streamWriter, message.SecondPosition);
                    break;

                case 4:
                    streamWriter.WriteInt32((int)message.Target.Type); streamWriter.WriteInt32(message.Target.Instance);
                    WriteVector3(streamWriter, message.Position);
                    break;

                case 5:
                    streamWriter.WriteInt32((int)message.Target.Type); streamWriter.WriteInt32(message.Target.Instance);
                    WriteVector3(streamWriter, message.Position);
                    streamWriter.WriteInt32((int)message.Attachment);
                    break;

                case 6:
                    streamWriter.WriteInt32((int)message.Target.Type); streamWriter.WriteInt32(message.Target.Instance);
                    streamWriter.WriteInt32((int)message.SecondTarget.Type); streamWriter.WriteInt32(message.SecondTarget.Instance);
                    streamWriter.WriteInt32((int)message.Attachment);
                    break;
            }
        }

        #endregion

        #region Methods

        private static Vector3 ReadVector3(StreamReader streamReader)
        {
            // Three floats in X, Y, Z order; the arguments are evaluated left to right.
            return new Vector3(streamReader.ReadSingle(), streamReader.ReadSingle(), streamReader.ReadSingle());
        }

        private static void WriteVector3(StreamWriter streamWriter, Vector3 value)
        {
            // AOSharp's Vector3 is a struct, so there is no null to default.
            Vector3 v = value;
            streamWriter.WriteSingle(v.X);
            streamWriter.WriteSingle(v.Y);
            streamWriter.WriteSingle(v.Z);
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
                    <GfxTriggerSerializer,
                        Func<StreamReader, SerializationContext, PropertyMetaData, object>>(o => o.Deserialize);
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
                    <GfxTriggerSerializer,
                        Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(o => o.Serialize);
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
