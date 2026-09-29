// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NanoEffectRecordListSerializer.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NanoEffectRecordListSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    using AOSharp.Common.GameData;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// Reads and writes an X3F1-counted list of <see cref="NanoEffectRecord"/>.
    /// </summary>
    /// <remarks>
    /// Ported 2026-09-29 from OmniCell's Serialization\Serializers\Custom\NanoEffects.cs (ReadList/WriteList).
    /// Registered in <see cref="SerializerResolverBuilder{T}"/> for the type <c>NanoEffectRecord[]</c>, so a message
    /// declares <c>[AoMember(n)] public NanoEffectRecord[] X</c> with no SerializeSize - the count is read here.
    /// Hand-written because each effect's argument block has a per-function layout (the client's spell-format
    /// table), which the attribute serializer cannot express.
    /// </remarks>
    public class NanoEffectRecordListSerializer : ISerializer
    {
        public Type Type => typeof(NanoEffectRecord[]);

        /// <summary>
        /// Reads the X3F1 count and that many effects.
        /// </summary>
        public static NanoEffectRecord[] ReadList(StreamReader streamReader)
        {
            var effects = new NanoEffectRecord[X3F1Count(streamReader.ReadInt32())];
            for (var i = 0; i < effects.Length; i++)
            {
                effects[i] = Read(streamReader);
            }

            return effects;
        }

        /// <summary>
        /// Writes the X3F1 count and the effects.
        /// </summary>
        public static void WriteList(StreamWriter streamWriter, NanoEffectRecord[] effects)
        {
            effects = effects ?? new NanoEffectRecord[0];
            streamWriter.WriteInt32((effects.Length + 1) * 0x3F1);
            foreach (var effect in effects)
            {
                Write(streamWriter, effect);
            }
        }

        /// <summary>
        /// Reads one effect.
        /// </summary>
        public static NanoEffectRecord Read(StreamReader streamReader)
        {
            var effect = new NanoEffectRecord();
            effect.Effect = new Identity((IdentityType)streamReader.ReadInt32(), streamReader.ReadInt32());
            effect.Version = streamReader.ReadInt32();
            effect.CriterionCount = streamReader.ReadInt32();
            if (effect.CriterionCount < 0 || effect.CriterionCount > 1000)
            {
                throw new InvalidOperationException(
                    string.Format("{0} criteria on one effect is not a count, so this list cannot be read", effect.CriterionCount));
            }

            effect.Criteria = new NanoEffectCriterion[effect.CriterionCount];
            for (var c = 0; c < effect.Criteria.Length; c++)
            {
                effect.Criteria[c] = new NanoEffectCriterion
                                     {
                                         Stat = (Stat)streamReader.ReadInt32(),
                                         Value = streamReader.ReadInt32(),
                                         Operator = streamReader.ReadInt32()
                                     };
            }

            effect.Hits = streamReader.ReadInt32();
            effect.Amount = streamReader.ReadInt32();
            effect.Target = streamReader.ReadInt32();
            effect.SpellList = streamReader.ReadInt32();
            effect.Arguments = ReadArguments(streamReader, (int)effect.Effect.Type);
            return effect;
        }

        /// <summary>
        /// Writes one effect.
        /// </summary>
        public static void Write(StreamWriter streamWriter, NanoEffectRecord effect)
        {
            streamWriter.WriteInt32((int)effect.Effect.Type);
            streamWriter.WriteInt32(effect.Effect.Instance);
            streamWriter.WriteInt32(effect.Version);
            var criteria = effect.Criteria ?? new NanoEffectCriterion[0];
            streamWriter.WriteInt32(criteria.Length);
            foreach (var criterion in criteria)
            {
                streamWriter.WriteInt32((int)criterion.Stat);
                streamWriter.WriteInt32(criterion.Value);
                streamWriter.WriteInt32(criterion.Operator);
            }

            streamWriter.WriteInt32(effect.Hits);
            streamWriter.WriteInt32(effect.Amount);
            streamWriter.WriteInt32(effect.Target);
            streamWriter.WriteInt32(effect.SpellList);
            streamWriter.WriteBytes(effect.Arguments ?? new byte[0]);
        }

        public object Deserialize(
            StreamReader streamReader,
            SerializationContext serializationContext,
            PropertyMetaData propertyMetaData = null)
        {
            return ReadList(streamReader);
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            WriteList(streamWriter, (NanoEffectRecord[])value);
        }

        public Expression DeserializerExpression(
            ParameterExpression streamReaderExpression,
            ParameterExpression serializationContextExpression,
            Expression assignmentTargetExpression,
            PropertyMetaData propertyMetaData)
        {
            var method = ReflectionHelper.GetMethodInfo<NanoEffectRecordListSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>(o => o.Deserialize);
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
            var method = ReflectionHelper.GetMethodInfo<NanoEffectRecordListSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(o => o.Serialize);
            return Expression.Call(
                Expression.Constant(this),
                method,
                streamWriterExpression,
                serializationContextExpression,
                Expression.Convert(valueExpression, typeof(object)),
                Expression.Constant(propertyMetaData, typeof(PropertyMetaData)));
        }

        private static byte[] ReadArguments(StreamReader streamReader, int function)
        {
            int[] arguments = NanoEffectRecordFormats.ArgumentsFor(function);
            if (arguments == null)
            {
                throw new InvalidOperationException(
                    string.Format("the client carries no spell format for game function {0}, so this effect cannot be read", function));
            }

            var block = new List<byte>();
            foreach (int argument in arguments)
            {
                byte[] head = streamReader.ReadBytes(4);
                block.AddRange(head);
                if (NanoEffectRecordFormats.KindOf(argument) != NanoEffectRecordFormats.StringArgument)
                {
                    continue;
                }

                int length = (head[0] << 24) | (head[1] << 16) | (head[2] << 8) | head[3];
                block.AddRange(streamReader.ReadBytes(length));
            }

            return block.ToArray();
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
