//using System;
//using System.IO;
//using System.Linq;
//using System.Linq.Expressions;
//using AOSharp.Common.GameData;
//using SmokeLounge.AOtomation.Messaging.GameData;
//using SmokeLounge.AOtomation.Messaging.Messages;
//using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

//namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
//{
//    class InspectSerializer : ISerializer
//    {
//        public Type Type { get; }

//        public object Deserialize(StreamReader streamReader, SerializationContext serializationContext, PropertyMetaData propertyMetaData = null)
//        {
//            InspectMessage inspectMessage = new InspectMessage();
//            inspectMessage.N3MessageType = (N3MessageType)streamReader.ReadInt32();
//            inspectMessage.Identity = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());
//            inspectMessage.Unknown = streamReader.ReadByte();
//            inspectMessage.Target = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());
//            long arrayLength = (streamReader.PeekUntilEnd() - 4) / 32;
//            inspectMessage.Slot = new InspectSlotInfo[arrayLength];

//            for (int i = 0; i < arrayLength; i++)
//            {
//                inspectMessage.Slot[i] = new InspectSlotInfo
//                {
//                    Unk = streamReader.ReadInt32(),
//                    EquipSlot = (EquipSlot)streamReader.ReadInt32(),
//                    Unk2 = streamReader.ReadInt32(),
//                    UniqueIdentity = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32()),
//                    HighId = streamReader.ReadInt32(),
//                    LowId = streamReader.ReadInt32(),
//                    Ql = streamReader.ReadInt32(),
//                };

//            }

//            return inspectMessage;
//        }

//        public Expression DeserializerExpression(ParameterExpression streamReaderExpression,
//            ParameterExpression serializationContextExpression, Expression assignmentTargetExpression,
//            PropertyMetaData propertyMetaData)
//        {
//            var deserializerMethodInfo =
//                ReflectionHelper
//                    .GetMethodInfo
//                        <InspectSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>
//                        (o => o.Deserialize);
//            var serializerExp = Expression.New(this.GetType());
//            var callExp = Expression.Call(
//                serializerExp,
//                deserializerMethodInfo,
//                new Expression[]
//                {
//                    streamReaderExpression, serializationContextExpression,
//                    Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
//                });

//            var assignmentExp = Expression.Assign(
//                assignmentTargetExpression, Expression.TypeAs(callExp, assignmentTargetExpression.Type));
//            return assignmentExp;
//        }

//        public void Serialize(StreamWriter streamWriter, SerializationContext serializationContext, object value, PropertyMetaData propertyMetaData = null)
//        {
//            throw new NotImplementedException();
//        }

//        public Expression SerializerExpression(ParameterExpression streamWriterExpression,
//            ParameterExpression serializationContextExpression, Expression valueExpression, PropertyMetaData propertyMetaData)
//        {
//            var serializerMethodInfo =
//                ReflectionHelper
//                    .GetMethodInfo
//                    <InspectSerializer,
//                        Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(o => o.Serialize);
//            var serializerExp = Expression.New(this.GetType());
//            var callExp = Expression.Call(
//                serializerExp,
//                serializerMethodInfo,
//                new[]
//                {
//                    streamWriterExpression, serializationContextExpression, valueExpression,
//                    Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
//                });
//            return callExp;
//        }
//    }
//}
