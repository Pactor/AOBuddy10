// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OperatorMessageSerializer.cs" company="AOBuddy10">
//   Added 2026-09-29 to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OperatorMessageSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System;
    using System.Linq.Expressions;

    using SmokeLounge.AOtomation.Messaging.Messages;

    /// <summary>
    /// Reads an <see cref="OperatorMessage"/> body as every byte left in the packet.
    /// </summary>
    /// <remarks>
    /// The packet has no count in front of its body, and the two directions differ in length (532 bytes from the
    /// server, 2 from the client), so neither the attribute serializer nor OmniCell's fixed length fits both. The
    /// message serializer hands each packet in its own stream, so "to the end of the stream" is "to the end of
    /// the packet".
    /// </remarks>
    public class OperatorMessageSerializer : ISerializer
    {
        public Type Type => typeof(OperatorMessage);

        public object Deserialize(
            StreamReader streamReader,
            SerializationContext serializationContext,
            PropertyMetaData propertyMetaData = null)
        {
            return new OperatorMessage { Payload = streamReader.ReadBytes(streamReader.PeekUntilEnd()) };
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            streamWriter.WriteBytes(((OperatorMessage)value).Payload ?? new byte[0]);
        }

        public Expression DeserializerExpression(
            ParameterExpression streamReaderExpression,
            ParameterExpression serializationContextExpression,
            Expression assignmentTargetExpression,
            PropertyMetaData propertyMetaData)
        {
            var method = ReflectionHelper.GetMethodInfo<OperatorMessageSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>(o => o.Deserialize);
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
            var method = ReflectionHelper.GetMethodInfo<OperatorMessageSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(o => o.Serialize);
            return Expression.Call(
                Expression.Constant(this),
                method,
                streamWriterExpression,
                serializationContextExpression,
                Expression.Convert(valueExpression, typeof(object)),
                Expression.Constant(propertyMetaData, typeof(PropertyMetaData)));
        }
    }
}
