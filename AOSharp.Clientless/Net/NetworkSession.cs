using AOSharp.Clientless.Common;
using AOSharp.Common;
using AOSharp.Common.GameData;
using AOSharp.Common.SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using Serilog.Core;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.Messages.SystemMessages;
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.Serializers;
using Stateless;
using MessagingStreamReader = SmokeLounge.AOtomation.Messaging.Serialization.StreamReader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;

namespace AOSharp.Clientless.Net
{
    public class SessionCookie
    {
        public uint Cookie1;
        public uint Cookie2;
    }

    public class NetworkSession
    {
        private NetworkStateMachine _stateMachine;
        private ZlibTcpClient _tcpClient;
        private MessageSerializer _serializer = new MessageSerializer();
        private HeaderSerializer _headerSerializer = new HeaderSerializer();

        internal bool InPlay => _stateMachine.IsInState(State.InPlay);
        internal bool Connected => _tcpClient.Connected;

        private SessionCookie _sessionCookie = null;
        private ushort _messageId = 1;

        private Logger _logger;

        private System.Timers.Timer _reconnectTimer;
        private System.Timers.Timer _pingTimer;

        internal Action<StateMachine<State, Trigger>.Transition> NetworkStateChanged;

        private Dictionary<SystemMessageType, Action<SystemMessage>> _sysMsgCallbacks;
        private Dictionary<N3MessageType, Action<N3Message>> _n3MsgCallbacks;
        private Dictionary<SystemMessageType, Action<SystemMessage>> _internalSysMsgCallbacks;
        private Dictionary<N3MessageType, Action<N3Message>> _internalN3MsgCallbacks;

        private ConcurrentQueue<byte[]> _inboundPacketQueue = new ConcurrentQueue<byte[]>();

        internal NetworkSession(Logger logger, Dictionary<SystemMessageType, Action<SystemMessage>> sysMsgCallbacks, Dictionary<N3MessageType, Action<N3Message>> n3MsgCallbacks)
        {
            _logger = logger;
            InitializeStateMachine();
            RegisterInternalSystemMessageHandlers();
            RegisterInternalN3MessageHandlers();

            _sysMsgCallbacks = sysMsgCallbacks;
            _n3MsgCallbacks = n3MsgCallbacks;
        }

        internal void Update()
        {
            while (_inboundPacketQueue.TryDequeue(out byte[] packet))
                ProcessCachedPacket(packet);
        }

        public void Connect()
        {
            _logger.Debug($"Requesting dimension info..");

            try 
            {
                DimensionInfo dimensionInfo = Client.Dimension == Dimension.RubiKa ? DimensionInfo.RubiKa : DimensionInfo.RubiKa2019;
                IPEndPoint loginHandlerEndpoint = new IPEndPoint(Dns.GetHostEntry(dimensionInfo.GameServerEndpoint.Host).AddressList[0], dimensionInfo.GameServerEndpoint.Port);
                _stateMachine.Fire(_stateMachine.ConnectTrigger, loginHandlerEndpoint);
            }
            catch(WebException ex)
            {
                _logger.Error($"Failed to retrieve dimension info. {ex}");
                _stateMachine.Fire(Trigger.FailedToRetreiveDimensionInfo);
            }
        }

        private void Connect(IPEndPoint endpoint)
        {
            _logger.Debug($"Connecting to {endpoint}");

            if (_tcpClient != null && _tcpClient.Connected)
                _tcpClient.Close();

            _tcpClient = new ZlibTcpClient(_logger);
            _tcpClient.Disconnected += (e, p) => _stateMachine.Fire(Trigger.Disconnect);
            _tcpClient.PacketRecv += (e, p) => _inboundPacketQueue.Enqueue(p);
            _tcpClient.BeginConnect(endpoint.Address, endpoint.Port, ConnectCallback, endpoint);
        }

        private void ConnectCallback(IAsyncResult result)
        {
            try
            {
                _tcpClient.EndConnect(result);
                _stateMachine.Fire(Trigger.OnTcpConnected);
            }
            catch (Exception exception)
            {
                IPEndPoint endpoint = result.AsyncState as IPEndPoint;

                _logger.Debug($"Failed to connect to {endpoint} -- {exception}");

                _stateMachine.Fire(Trigger.OnTcpConnectError);
            }
        }

        public void Disconnect()
        {
            _stateMachine.Fire(Trigger.Disconnect);
        }

        public void Send(MessageBody messageBody)
        {
            if (messageBody is N3Message n3Message)
                n3Message.Identity = new Identity(IdentityType.SimpleChar, Client.LocalDynelId);

            Message message = new Message
            {
                Body = messageBody,
                Header = new Header
                {
                    PacketType = messageBody.PacketType,
                    Sender = Client.LocalDynelId,
                    Receiver = messageBody.PacketType == PacketType.SystemMessage ? 1 : 2
                }
            };

            Send(message);
        }

        public void Send(Message message)
        {
            message.Header.MessageId = _messageId;

            using (MemoryStream stream = new MemoryStream())
            {
                _serializer.Serialize(stream, message);
                _tcpClient.Send(stream.ToArray());
            }

            _messageId++;

            if (_messageId == 0xFFFF)
                _messageId = 1;
        }

        private void ProcessCachedPacket(byte[] packet)
        {
            try
            {
                // WORKAROUND (AOSharpSDK 1.0.89): the SimpleCharFullUpdate reader
                // baked into the NuGet AOSharp.Common.dll is an older, incomplete
                // version that throws EndOfStreamException on pet-type / complex
                // NPC spawns, so those dynels never register. There is no API to
                // replace a serializer, so detect those packets up front and parse
                // them with our corrected reader (see SimpleCharFullUpdateReader),
                // then let the result flow through the normal dispatch below. Every
                // other packet takes the unchanged MessageSerializer path.
                Message message;
                if (!TryDeserializeSimpleCharFullUpdate(packet, out message))
                {
                    // WORKAROUND (AOSharpSDK 1.0.89): the ChestFullUpdate reader baked into the NuGet
                    // AOSharp.Common.dll is an older, broken version that throws OverflowException on the
                    // mission / corpse containers the server sends on zone-in (a huge count is read from
                    // the wrong offset). The correct field layout is known (OmniCell's
                    // ChestItemFullUpdateMessage), but the bot doesn't use chest contents, so we simply
                    // DROP these packets rather than let the broken serializer throw and spam the console.
                    // When looting is needed later, add a corrected reader here like the SimpleChar one.
                    if (IsN3MessageType(packet, N3MessageType.ChestFullUpdate))
                        return;

                    message = _serializer.Deserialize(packet);
                }

                if (message == null)
                    return;

                if (message.Header.Sender != Client.ServerId)
                    Client.ServerId = message.Header.Sender;

                Client.MessageReceived?.Invoke(null, message);

                if (message.Header.PacketType == PacketType.InitiateCompressionMessage)
                {
                    OnInitiateCompressionMessage();
                }
                else if (message.Header.PacketType == PacketType.PingMessage)
                {
                    Pong(message);
                }
                else if (message.Header.PacketType == PacketType.SystemMessage)
                {
                    SystemMessage sysMsg = (SystemMessage)message.Body;

                    if (_sysMsgCallbacks.TryGetValue(sysMsg.SystemMessageType, out Action<SystemMessage> callback))
                        callback.Invoke(sysMsg);

                    if (_internalSysMsgCallbacks.TryGetValue(sysMsg.SystemMessageType, out Action<SystemMessage> internalCallback))
                        internalCallback.Invoke(sysMsg);
                }
                else if (message.Header.PacketType == PacketType.N3Message)
                {
                    Client.PacketReceived?.Invoke(null, packet);

                    N3Message n3Msg = (N3Message)message.Body;

                    if (_n3MsgCallbacks.TryGetValue(n3Msg.N3MessageType, out Action<N3Message> callback))
                        callback.Invoke(n3Msg);

                    if (_internalN3MsgCallbacks.TryGetValue(n3Msg.N3MessageType, out Action<N3Message> internalCallback))
                        internalCallback.Invoke(n3Msg);
                }
            }
            catch(Exception e)
            {
                if (Client.LogDeserializationErrors)
                    return;

                _logger.Error($"Failed to deserialize packet: {packet.ToHexString()}");
                _logger.Error(e.ToString());
            }
        }

        /// <summary>
        /// If <paramref name="packet"/> is an N3 SimpleCharFullUpdate, parse it with
        /// the corrected reader and return true; otherwise return false so the caller
        /// falls back to the stock MessageSerializer. See SimpleCharFullUpdateReader
        /// for why this workaround exists.
        /// </summary>
        private bool TryDeserializeSimpleCharFullUpdate(byte[] packet, out Message message)
        {
            message = null;

            // Need at least a 16-byte header plus the 4-byte N3 message type.
            if (packet == null || packet.Length < 20)
                return false;

            Header header;
            try
            {
                header = DeserializeHeader(packet);
            }
            catch
            {
                // Let the stock path (and its logging) handle anything malformed.
                return false;
            }

            if (header.PacketType != PacketType.N3Message)
                return false;

            // The N3 message type sits immediately after the 16-byte header.
            int n3MessageType;
            using (MemoryStream peekStream = new MemoryStream(packet))
            using (MessagingStreamReader peekReader = new MessagingStreamReader(peekStream))
            {
                peekReader.Position = 16;
                n3MessageType = peekReader.ReadInt32();
            }

            if (n3MessageType != (int)N3MessageType.SimpleCharFullUpdate)
                return false;

            // Confirmed SimpleCharFullUpdate. Parse it with the corrected reader.
            // Any failure here propagates to ProcessCachedPacket's catch and is
            // logged once, exactly as a stock deserialize failure would be.
            using (MemoryStream bodyStream = new MemoryStream(packet))
            using (MessagingStreamReader bodyReader = new MessagingStreamReader(bodyStream))
            {
                bodyReader.Position = 16;
                SimpleCharFullUpdateMessage body = SimpleCharFullUpdateReader.Read(bodyReader);
                message = new Message { Header = header, Body = body };
            }

            return true;
        }

        /// <summary>
        /// True if <paramref name="packet"/> is an N3 message of the given type. Peeks the 4-byte N3
        /// type that sits right after the 16-byte header without deserializing the (possibly broken) body.
        /// </summary>
        private bool IsN3MessageType(byte[] packet, N3MessageType type)
        {
            if (packet == null || packet.Length < 20)
                return false;

            Header header;
            try
            {
                header = DeserializeHeader(packet);
            }
            catch
            {
                return false;
            }

            if (header.PacketType != PacketType.N3Message)
                return false;

            using (MemoryStream peekStream = new MemoryStream(packet))
            using (MessagingStreamReader peekReader = new MessagingStreamReader(peekStream))
            {
                peekReader.Position = 16;
                return peekReader.ReadInt32() == (int)type;
            }
        }

        private Header DeserializeHeader(byte[] packet)
        {
            byte[] headerBytes = new byte[16];
            Array.Copy(packet, 0, headerBytes, 0, 16);

            using (MemoryStream headerStream = new MemoryStream(headerBytes))
            using (MessagingStreamReader headerReader = new MessagingStreamReader(headerStream))
                return (Header)_headerSerializer.Deserialize(headerReader, null);
        }

        private void Reconnect()
        {
            _tcpClient.Close();
            _sessionCookie = null;
            _messageId = 1;

            if (Client.Config.AutoReconnect)
                Task.Delay(Client.Config.ReconnectDelay).ContinueWith(t => Connect());
            else
                _stateMachine.Fire(Trigger.Stop);
        }

        private void InitializeStateMachine()
        {
            _stateMachine = new NetworkStateMachine(_logger);

            _stateMachine.OnTransitioned(OnNetworkStateTransition);

            _stateMachine.Configure(State.Idle)
                .Permit(Trigger.Connect, State.Connecting);

            _stateMachine.Configure(State.Disconnected)
                .OnEntry(() => Reconnect())
                .Ignore(Trigger.Disconnect)
                .Permit(Trigger.Connect, State.Connecting)
                .PermitReentry(Trigger.FailedToRetreiveDimensionInfo);

            _stateMachine.Configure(State.Connecting)
                .OnEntryFrom(_stateMachine.ConnectTrigger, (endpoint) => Connect(endpoint))
                .OnEntryFrom(_stateMachine.ConnectErrorTrigger, (endpoint, exception) => Connect(endpoint))
                .Permit(Trigger.OnTcpConnectError, State.Disconnected)
                .Permit(Trigger.Disconnect, State.Disconnected)
                .Permit(Trigger.OnTcpConnected, State.Connected);

            _stateMachine.Configure(State.Connected)
                .OnEntryFrom(Trigger.OnTcpConnected, () =>
                {
                    _tcpClient.BeginReceiving();
                    _stateMachine.Fire(Trigger.ConnectionEstablished);
                })
                .PermitIf(Trigger.ConnectionEstablished, State.Authenticating, () => _sessionCookie == null)
                .PermitIf(Trigger.ConnectionEstablished, State.Zoning, () => !(_sessionCookie == null))
                .Permit(Trigger.OnTcpConnectionError, State.Disconnected)
                .Permit(Trigger.Connect, State.Connecting)
                .Permit(Trigger.Disconnect, State.Disconnected);

            _stateMachine.Configure(State.Authenticating)
                .SubstateOf(State.Connected)
                .OnEntry(() =>
                {
                    Send(new UserLoginMessage
                    {
                        UserName = Client.Credentials.Username,
                        ClientVersion = Client.Dimension == Dimension.RubiKa ? DimensionInfo.RubiKa.Version : DimensionInfo.RubiKa2019.Version
                    });
                })
                .Permit(Trigger.Disconnect, State.Disconnected)
                .Permit(Trigger.FailedToLogin, State.Disconnected);

            _stateMachine.Configure(State.Zoning)
                .OnEntry(() =>
                {
                    Client.OnTeleportStart();
                })
                .SubstateOf(State.Connected)
                .Permit(Trigger.CharInPlay, State.InPlay)
                .Permit(Trigger.Disconnect, State.Disconnected);

            _stateMachine.Configure(State.InPlay)
                .SubstateOf(State.Connected)
                .Ignore(Trigger.CharInPlay)
                .Permit(Trigger.Connect, State.Connecting)
                .Permit(Trigger.Disconnect, State.Disconnected);
        }

        private void Pong(Message pingMsg)
        {
            PingMessage pingBody = (PingMessage)pingMsg.Body;

            Message pongMsg = new Message
            {
                Body = new PingMessage
                {
                    PingMessageType = PingMessageType.Pong,
                    ServerTime = pingBody.ServerTime,
                    UpTime1 = pingBody.UpTime1,
                    UpTime2 = pingBody.UpTime2,
                    Unk2 = pingBody.Unk2,
                },
                Header = new Header
                {
                    PacketType = PacketType.PingMessage,
                    Sender = Client.LocalDynelId,
                    Receiver = pingMsg.Header.Sender
                }
            };

            Send(pongMsg);
        }

        private void OnInitiateCompressionMessage()
        {
            Send(new ZoneLoginMessage
            {
                CharacterId = Client.LocalDynelId,
                Cookie1 = _sessionCookie.Cookie1,
                Cookie2 = _sessionCookie.Cookie2
            });
        }

        private void RegisterInternalSystemMessageHandlers()
        {
            _internalSysMsgCallbacks = new Dictionary<SystemMessageType, Action<SystemMessage>>();

            _internalSysMsgCallbacks.Add(SystemMessageType.ZoneInfo, (msg) =>
            {
                ZoneInfoMessage zoneInfoMsg = (ZoneInfoMessage)msg;

                _sessionCookie = new SessionCookie
                {
                    Cookie1 = zoneInfoMsg.Cookie1,
                    Cookie2 = zoneInfoMsg.Cookie2
                };

                _stateMachine.Fire(_stateMachine.ConnectTrigger, new IPEndPoint(zoneInfoMsg.ServerIpAddress, zoneInfoMsg.ServerPort));
            });

            _internalSysMsgCallbacks.Add(SystemMessageType.ZoneRedirection, (msg) =>
            {
                ZoneRedirectionMessage zoneRedMsg = (ZoneRedirectionMessage)msg;

                _logger.Debug($"ZoneRediction to {zoneRedMsg.ServerIpAddress}:{zoneRedMsg.ServerPort}");

                _stateMachine.Fire(_stateMachine.ConnectTrigger, new IPEndPoint(zoneRedMsg.ServerIpAddress, zoneRedMsg.ServerPort));
            });

            _internalSysMsgCallbacks.Add(SystemMessageType.LoginError, (msg) =>
            {
                LoginErrorMessage loginErrorMsg = (LoginErrorMessage)msg;

                _logger.Debug($"Failed to login: {loginErrorMsg.Error}");

                _stateMachine.Fire(Trigger.FailedToLogin);
            });
        }

        private void RegisterInternalN3MessageHandlers()
        {
            _internalN3MsgCallbacks = new Dictionary<N3MessageType, Action<N3Message>>();

            _internalN3MsgCallbacks.Add(N3MessageType.FullCharacter, (msg) =>
            {
                _stateMachine.Fire(Trigger.CharInPlay);
            });
        }

        private void OnNetworkStateTransition(StateMachine<State, Trigger>.Transition transition)
        {
            NetworkStateChanged?.Invoke(transition);
        }
    }
}
