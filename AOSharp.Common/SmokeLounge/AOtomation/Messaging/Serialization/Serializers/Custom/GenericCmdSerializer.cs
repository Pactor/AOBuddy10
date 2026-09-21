//using System;
//using System.Linq.Expressions;
//using AOSharp.Common.GameData;
//using SmokeLounge.AOtomation.Messaging.GameData;
//using SmokeLounge.AOtomation.Messaging.Messages;
//using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

//namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
//{

//    class GenericCmdSerializer : ISerializer
//    {
//        public Type Type { get; }

//        public object Deserialize(StreamReader streamReader, SerializationContext serializationContext,
//            PropertyMetaData propertyMetaData = null)
//        {
//            GenericCmdMessage genericCmdMessage = new GenericCmdMessage();
//            genericCmdMessage.N3MessageType = (N3MessageType) streamReader.ReadInt32();
//            genericCmdMessage.Identity = new Identity
//            {
//                Type = (IdentityType)streamReader.ReadInt32(),
//                Instance = streamReader.ReadInt32()
//            };
//            genericCmdMessage.Unknown = streamReader.ReadByte();
//            genericCmdMessage.Temp1 = streamReader.ReadInt32();
//            genericCmdMessage.Count = streamReader.ReadInt32();
//            genericCmdMessage.Action = (GenericCmdAction)streamReader.ReadInt32();
//            genericCmdMessage.Temp4 = streamReader.ReadInt32();
//            genericCmdMessage.User = new Identity
//            {
//                Type = (IdentityType) streamReader.ReadInt32(),
//                Instance = streamReader.ReadInt32()
//            };
            
//            if (genericCmdMessage.Action == GenericCmdAction.UseItemOnItem)
//            {
//                genericCmdMessage.Source = new Identity
//                {
//                    Type = (IdentityType)streamReader.ReadInt32(),
//                    Instance = streamReader.ReadInt32()
//                };
                
//                genericCmdMessage.Target = new Identity
//                {
//                    Type = (IdentityType)streamReader.ReadInt32(),
//                    Instance = streamReader.ReadInt32()
//                };
//            }
//            else
//            {
//                genericCmdMessage.Source = Identity.None;
//                genericCmdMessage.Target = new Identity
//                {
//                    Type = (IdentityType)streamReader.ReadInt32(),
//                    Instance = streamReader.ReadInt32()
//                };
//            }

//            return genericCmdMessage;
//        }

//        public Expression DeserializerExpression(ParameterExpression streamReaderExpression,
//            ParameterExpression serializationContextExpression, Expression assignmentTargetExpression,
//            PropertyMetaData propertyMetaData)
//        {
//            var deserializerMethodInfo =
//                ReflectionHelper
//                    .GetMethodInfo
//                        <GenericCmdSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>
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

//        public void Serialize(StreamWriter streamWriter, SerializationContext serializationContext, object value,
//            PropertyMetaData propertyMetaData = null)
//        {
//            var genericCmd = (GenericCmdMessage)value;
//            streamWriter.WriteInt32((int)genericCmd.N3MessageType);
//            streamWriter.WriteInt32((int)genericCmd.Identity.Type);
//            streamWriter.WriteInt32(genericCmd.Identity.Instance);
//            streamWriter.WriteByte(genericCmd.Unknown);
//            streamWriter.WriteInt32(genericCmd.Temp1);
//            streamWriter.WriteInt32(genericCmd.Count);
//            streamWriter.WriteInt32((int)genericCmd.Action);
//            streamWriter.WriteInt32(genericCmd.Temp4);
//            streamWriter.WriteInt32((int)genericCmd.User.Type);
//            streamWriter.WriteInt32(genericCmd.User.Instance);

//            if (genericCmd.Action == GenericCmdAction.UseItemOnItem)
//            {
//                streamWriter.WriteInt32((int)genericCmd.Source.Type);
//                streamWriter.WriteInt32(genericCmd.Source.Instance);
//            }

//            streamWriter.WriteInt32((int)genericCmd.Target.Type);
//            streamWriter.WriteInt32(genericCmd.Target.Instance);
//        }

//        public Expression SerializerExpression(ParameterExpression streamWriterExpression,
//            ParameterExpression serializationContextExpression, Expression valueExpression, PropertyMetaData propertyMetaData)
//        {
//            var serializerMethodInfo =
//                ReflectionHelper
//                    .GetMethodInfo
//                    <GenericCmdSerializer,
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
