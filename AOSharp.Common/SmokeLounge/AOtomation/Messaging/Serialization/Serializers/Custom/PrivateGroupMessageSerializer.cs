using System;
using System.IO;
using System.Linq.Expressions;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    class PrivateGroupMessageSerializer : ISerializer
    {
        public Type Type { get; }

        public object Deserialize(StreamReader streamReader, SerializationContext serializationContext,
            PropertyMetaData propertyMetaData = null)
        {
            PrivateGroupMessage groupMsg = new PrivateGroupMessage();
            groupMsg.ChannelId = streamReader.ReadUInt32();
            groupMsg.Sender = streamReader.ReadUInt32();
            groupMsg.Text = streamReader.ReadString(streamReader.ReadUInt16());
            groupMsg.Unk1 = streamReader.ReadByte();
            groupMsg.Unk2 = streamReader.ReadByte();

            return groupMsg;
        }

        public Expression DeserializerExpression(ParameterExpression streamReaderExpression,
            ParameterExpression serializationContextExpression, Expression assignmentTargetExpression,
            PropertyMetaData propertyMetaData)
        {
            var deserializerMethodInfo =
                ReflectionHelper
                    .GetMethodInfo
                        <PrivateGroupMessageSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>
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

        public void Serialize(StreamWriter streamWriter, SerializationContext serializationContext, object value,
            PropertyMetaData propertyMetaData = null)
        {
            var groupMsg = (PrivateGroupMessage)value;
            streamWriter.WriteUInt32(groupMsg.ChannelId);
            streamWriter.WriteUInt16((ushort)groupMsg.Text.Length);
            streamWriter.WriteString(groupMsg.Text);
            streamWriter.WriteByte(groupMsg.Unk1);
            streamWriter.WriteByte(groupMsg.Unk2);
        }

        public Expression SerializerExpression(ParameterExpression streamWriterExpression,
            ParameterExpression serializationContextExpression, Expression valueExpression, PropertyMetaData propertyMetaData)
        {
            var serializerMethodInfo =
                ReflectionHelper
                    .GetMethodInfo
                    <PrivateGroupMessageSerializer,
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
    }
}
