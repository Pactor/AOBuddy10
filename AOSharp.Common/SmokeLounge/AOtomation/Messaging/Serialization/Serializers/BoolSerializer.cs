// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BoolSerializer.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the BoolSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers
{
    using System;
    using System.Linq.Expressions;

    public class BoolSerializer : ISerializer
    {
        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public BoolSerializer()
        {
            this.type = typeof(bool);
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
            return streamReader.ReadBool();
        }

        public Expression DeserializerExpression(
            ParameterExpression streamReaderExpression, 
            ParameterExpression serializationContextExpression, 
            Expression assignmentTargetExpression, 
            PropertyMetaData propertyMetaData)
        {
            var readMethodInfo = ReflectionHelper.GetMethodInfo<StreamReader, Func<bool>>(o => o.ReadBool);
            var callReadExp = Expression.Call(streamReaderExpression, readMethodInfo);
            if (assignmentTargetExpression.Type.IsAssignableFrom(this.type))
            {
                return Expression.Assign(assignmentTargetExpression, callReadExp);
            }

            var assignmentExp = Expression.Assign(
                assignmentTargetExpression, Expression.Convert(callReadExp, assignmentTargetExpression.Type));
            return assignmentExp;
        }

        public void Serialize(
            StreamWriter streamWriter, 
            SerializationContext serializationContext, 
            object value, 
            PropertyMetaData propertyMetaData = null)
        {
            streamWriter.WriteBool((bool)value);
        }

        public Expression SerializerExpression(
            ParameterExpression streamWriterExpression, 
            ParameterExpression serializationContextExpression, 
            Expression valueExpression, 
            PropertyMetaData propertyMetaData)
        {
            var writeMethodInfo = ReflectionHelper.GetMethodInfo<StreamWriter, Action<bool>>(o => o.WriteBool);
            if (valueExpression.Type.IsAssignableFrom(this.type))
            {
                return Expression.Call(streamWriterExpression, writeMethodInfo, new[] { valueExpression });
            }

            var callWriteExp = Expression.Call(
                streamWriterExpression, 
                writeMethodInfo, 
                new Expression[] { Expression.Convert(valueExpression, this.type) });
            return callWriteExp;
        }

        #endregion
    }
}