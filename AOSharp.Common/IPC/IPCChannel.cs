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
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace AOSharp.Core.IPC
{
    public abstract class IPCChannelBase
    {
        protected abstract int _localDynelId { get; }

        private static IPAddress MulticastIP = IPAddress.Parse("224.0.0.111");
        private IPEndPoint _localEndPoint = new IPEndPoint(IPAddress.Any, Port);
        private IPEndPoint _remoteEndPoint = new IPEndPoint(MulticastIP, Port);
        private const int Port = 1911;
        private const ushort PacketPrefix = 0xFFFF;

        private byte _channelId;
        private UdpClient _udpClient;

        private static SerializerResolver _serializerResolver = new SerializerResolverBuilder<IPCMessage>().Build();
        private static TypeInfo _typeInfo = new TypeInfo(typeof(IPCMessage));
        private static PacketInspector _packetInspector;

        private ConcurrentQueue<byte[]> _packetQueue = new ConcurrentQueue<byte[]>();
        private Dictionary<int, List<Action<int, IPCMessage>>> _callbacks = new Dictionary<int, List<Action<int, IPCMessage>>>();
        private static List<IPCChannelBase> _ipcChannels = new List<IPCChannelBase>();

        protected IPCChannelBase(byte channelId)
        {
            _channelId = channelId;

            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(_localEndPoint);

            _udpClient.JoinMulticastGroup(MulticastIP);
            _udpClient.BeginReceive(ReceiveCallback, null);

            _packetInspector = new PacketInspector(_typeInfo);
            _ipcChannels.Add(this);
        }

        ~IPCChannelBase()
        {
            _ipcChannels.Remove(this);
        }

        protected static void Update()
        {
            try
            {
                foreach (IPCChannelBase ipcChannel in _ipcChannels)
                    ipcChannel.ProcessQueue();
            }
            catch (Exception e)
            {
            }
        }

        private void ProcessQueue()
        {
            while (_packetQueue.TryDequeue(out byte[] msgBytes))
                ProcessIPCMessage(msgBytes);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            byte[] receiveBytes = _udpClient.EndReceive(ar, ref _localEndPoint);
            _udpClient.BeginReceive(ReceiveCallback, null);

            if (receiveBytes.Length < 11)
                return;

            _packetQueue.Enqueue(receiveBytes);
        }

        private void ProcessIPCMessage(byte[] msgBytes)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(msgBytes))
                {
                    StreamReader reader = new StreamReader(stream) { Position = 0 };

                    if (reader.ReadUInt16() != 0xFFFF)
                        return;

                    ushort len = reader.ReadUInt16();

                    if (len != msgBytes.Length)
                        return;

                    byte channelId = reader.ReadByte();

                    if (channelId != _channelId)
                        return;

                    int charId = reader.ReadInt32();

                    if (charId == _localDynelId)
                        return;

                    reader.Position = 2;
                    TypeInfo subTypeInfo = _packetInspector.FindSubType(reader, out int opCode);

                    if (subTypeInfo == null)
                        return;

                    var serializer = _serializerResolver.GetSerializer(subTypeInfo.Type);
                    if (serializer == null)
                        return;

                    reader.Position = 11;
                    SerializationContext serializationContext = new SerializationContext(_serializerResolver);

                    IPCMessage message = (IPCMessage)serializer.Deserialize(reader, serializationContext);

                    if (_callbacks.ContainsKey(opCode))
                        foreach(var callback in _callbacks[opCode])
                            callback?.Invoke(charId, message);
                }
            }
            catch (Exception e)
            {
            }
        }

        public void Broadcast(IPCMessage msg)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ISerializer serializer = _serializerResolver.GetSerializer(msg.GetType());

                if (serializer == null)
                    return;

                int opcode = ((AoContractAttribute)msg.GetType().GetCustomAttributes(typeof(AoContractAttribute)).FirstOrDefault()).Identifier;

                SerializationContext serializationContext = new SerializationContext(_serializerResolver);
                StreamWriter writer = new StreamWriter(stream) { Position = 0 };
                writer.WriteUInt16(PacketPrefix);
                writer.WriteInt16(0);
                writer.WriteByte(_channelId);
                writer.WriteInt32(_localDynelId);
                writer.WriteInt16((short)opcode);
                serializer.Serialize(writer, serializationContext, msg);
                long length = writer.Position;
                writer.Position = 2;
                writer.WriteInt16((short)length);
                writer.Dispose();

                byte[] serialized = stream.ToArray();
                _udpClient.Send(serialized, serialized.Length, _remoteEndPoint);
            }
        }

        public void RegisterCallback(int opCode, Action<int, IPCMessage> callback)
        {
            if (!_callbacks.ContainsKey(opCode))
                _callbacks[opCode] = new List<Action<int, IPCMessage>>();

            _callbacks[opCode].Add(callback);
        }

        public static void LoadMessages(Assembly assembly)
        {
            _typeInfo.InitializeSubTypesForAssembly(assembly);
        }

        public bool SetChannelId(byte channelId)
        {
            if (_ipcChannels.Any(x => x._channelId == channelId))
                return false;

            _channelId = channelId;
            return true;
        }
    }
}
