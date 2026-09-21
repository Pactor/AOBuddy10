// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SerializerResolverBuilder.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SerializerResolverBuilder type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
    using SmokeLounge.AOtomation.Messaging.Serialization.Serializers;
    using SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom;

    public abstract class SerializerResolverBuilder
    {
        #region Public Methods and Operators

        public abstract SerializerResolver Build();

        #endregion

        #region Methods

        internal abstract ISerializer GetSerializer(Type type);

        #endregion
    }

    public class SerializerResolverBuilder<T> : SerializerResolverBuilder
    {
        #region Fields

        private readonly ConcurrentDictionary<Type, ISerializer> serializers;

        #endregion

        #region Constructors and Destructors

        public SerializerResolverBuilder()
        {
            this.serializers = new ConcurrentDictionary<Type, ISerializer>();
            this.serializers.TryAdd(typeof(bool), new BoolSerializer());
            this.serializers.TryAdd(typeof(byte), new ByteSerializer());
            this.serializers.TryAdd(typeof(short), new Int16Serializer());
            this.serializers.TryAdd(typeof(int), new Int32Serializer());
            this.serializers.TryAdd(typeof(long), new Int64Serializer());
            this.serializers.TryAdd(typeof(IPAddress), new IPAddressSerializer());
            this.serializers.TryAdd(typeof(float), new SingleSerializer());
            this.serializers.TryAdd(typeof(double), new DoubleSerializer());
            this.serializers.TryAdd(typeof(string), new StringSerializer());
            this.serializers.TryAdd(typeof(ushort), new UInt16Serializer());
            this.serializers.TryAdd(typeof(uint), new UInt32Serializer());
            this.serializers.TryAdd(typeof(PlayfieldVendorInfo), new PlayfieldVendorInfoSerializer());
            this.serializers.TryAdd(typeof(SimpleCharFullUpdateMessage), new SimpleCharFullUpdateSerializer());
            //this.serializers.TryAdd(typeof(GenericCmdMessage), new GenericCmdSerializer());
            this.serializers.TryAdd(typeof(GroupMsgMessage), new GroupMessageSerializer());
            this.serializers.TryAdd(typeof(PlayfieldTowerUpdateClientMessage), new PlayfieldTowerUpdateClientSerializer());
            this.serializers.TryAdd(typeof(LookupMessage), new LookupMessageSerializer());
            this.serializers.TryAdd(typeof(FriendStatusMessage), new FriendStatusSerializer());
            this.serializers.TryAdd(typeof(PrivateGroupMessage), new PrivateGroupMessageSerializer());
        }

        #endregion

        #region Public Methods and Operators

        public override SerializerResolver Build()
        {
            var rootType = typeof(T);

            var subTypes = rootType.Assembly.GetTypes().Where(rootType.IsAssignableFrom);

            foreach (var subType in subTypes)
            {
                if (this.serializers.ContainsKey(subType))
                {
                    continue;
                }

                var serializer = this.CreateSerializer(subType);
                if (serializer != null)
                {
                    this.serializers.TryAdd(subType, serializer);
                }
            }

            var serializationContext = new SerializerResolver(this);
            return serializationContext;
        }

        #endregion

        #region Methods

        internal override ISerializer GetSerializer(Type type)
        {
            ISerializer serializer;
            if (this.serializers.TryGetValue(type, out serializer))
            {
                return serializer;
            }

            if (type.IsEnum)
            {
                var enumType = type.GetEnumUnderlyingType();
                if (this.serializers.TryGetValue(enumType, out serializer))
                {
                    return serializer;
                }
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                serializer = this.GetSerializer(elementType);
                if (serializer == null)
                {
                    return null;
                }

                var arraySerializer = new ArraySerializer(type, serializer);
                this.serializers.TryAdd(type, arraySerializer);
                return arraySerializer;
            }

            serializer = this.CreateSerializer(type);
            if (serializer != null)
            {
                this.serializers.TryAdd(type, serializer);
            }

            return serializer;
        }

        private ISerializer CreateSerializer(Type type)
        {
            if (type.IsAbstract)
            {
                return null;
            }

            var typeSerializerBuilder = new TypeSerializerBuilder(type, this.GetSerializer);
            var typeSerializer = new TypeSerializer(type, typeSerializerBuilder);
            return typeSerializer;
        }

        #endregion
    }
}