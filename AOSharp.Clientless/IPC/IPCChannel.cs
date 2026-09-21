using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.Serializers;
using StreamWriter = SmokeLounge.AOtomation.Messaging.Serialization.StreamWriter;
using StreamReader = SmokeLounge.AOtomation.Messaging.Serialization.StreamReader;
using TypeInfo = SmokeLounge.AOtomation.Messaging.Serialization.TypeInfo;
using AOSharp.Common.Unmanaged.Imports;
using System.Reflection;
using AOSharp.Clientless;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
using AOSharp.Core.IPC;

namespace AOSharp.Clientless
{
    public class IPCChannel<TOpcode> : IPCChannel where TOpcode : Enum, IConvertible
    {
        public IPCChannel(byte channelId) : base(channelId)
        {
        }

        public void RegisterCallback(TOpcode opCode, Action<int, IPCMessage> callback) => RegisterCallback(opCode.ToInt16(null), callback);
    }

    public class IPCChannel : IPCChannelBase
    {
        public IPCChannel(byte channelId) : base(channelId)
        {
        }

        internal static void UpdateInternal() => Update();

        protected override int _localDynelId => Client.LocalDynelId;
    }
}
