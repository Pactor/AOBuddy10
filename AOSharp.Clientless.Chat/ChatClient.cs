using AOSharp.Clientless.Chat.Net;
using AOSharp.Clientless.Common;
using AOSharp.Common.GameData;
using Serilog.Core;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;
using Stateless;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Clientless.Chat
{
    public class ChatConfig
    {
        public bool AutoReconnect = true;
        public int ReconnectDelay = 5000; //TODO: implement exponential backoff
        public int PingInterval = 60;
    }

    public class ChatClient : NetworkSession
    {
        public readonly ChatConfig Config;
        public readonly string Character;

        protected readonly Logger _logger;
        private UpdateLoop _updateLoop;
        private double _timeTillNextPing;
        private const int AvailableMessagePool = 4;
        private AutoResetInterval _availMessageInterval = new AutoResetInterval(2000);
        private int AvailableMessageCount = AvailableMessagePool;

        public uint CharId { internal set; get; }
        public Dictionary<uint, bool> FriendStatuses = new Dictionary<uint, bool>();
        public Dictionary<string, uint> NameToIdMap = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);   // names are case-insensitive: a tell to "scty" waited forever on the server's "Scty"
        public Dictionary<uint, string> IdToNameMap = new Dictionary<uint, string>();
        internal Dictionary<int, string> ChannelToIdMap = new Dictionary<int, string>();

        private Queue<PendingPrivateMessage> _outboundPrivMessageQueue = new Queue<PendingPrivateMessage>();

        internal readonly Credentials Credentials;

        public ChatClient(Credentials credentials, string character, Dimension dimension, Logger logger) : this(credentials, character, dimension, logger, new ChatConfig())
        {
        }

        public ChatClient(Credentials credentials, string character, Dimension dimension, Logger logger, ChatConfig config) : base(dimension, logger)
        {
            Credentials = credentials;
            Character = character;
            _logger = logger;
            Config = config;

            _timeTillNextPing = Config.PingInterval;
        }

        public int GetChannelIdByIndex(int index) => ChannelToIdMap.ElementAt(index).Key;

        public void Init(bool useBuiltInLooper = true)
        {
            if (useBuiltInLooper)
            {
                _updateLoop = new UpdateLoop(Update);
                _updateLoop.Start();
            }

            Connect();
        }

        public override void Disconnect()
        {
            _updateLoop?.Stop();
            base.Disconnect();
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            _timeTillNextPing -= deltaTime;

            if (_timeTillNextPing <= 0)
            {
                Send(new ChatPingMessage());
                _timeTillNextPing = Config.PingInterval;
            }

            if (_availMessageInterval.Elapsed)
                AvailableMessageCount = Math.Min(++AvailableMessageCount, AvailableMessagePool);

            foreach(var pendingMsg in _outboundPrivMessageQueue.Where(x => !x.RecipientId.HasValue))
                if (!pendingMsg.RecipientId.HasValue && NameToIdMap.TryGetValue(pendingMsg.RecipientName, out uint recipientId))
                    pendingMsg.RecipientId = recipientId;

            while (_outboundPrivMessageQueue.Any(x => x.RecipientId.HasValue) && AvailableMessageCount > 0)
            {
                var msg = _outboundPrivMessageQueue.Dequeue();

                if (msg.RecipientId.HasValue)
                {
                    SendPrivateMessage(msg);
                    continue;
                }

                _outboundPrivMessageQueue.Enqueue(msg);
            }
        }

        public void RemoveChannelId(int channelId)
        {
            if (ChannelToIdMap.ContainsKey(channelId))
                ChannelToIdMap.Remove(channelId); 
        }

        public bool TryGetChannelId(ChannelType channelType, out int channelId)
        {
            Type enumType = channelType.GetType();
            FieldInfo field = enumType.GetField(channelType.ToString());
            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));

            channelId = 0;

            foreach (var channel in ChannelToIdMap)
            {
                if (channel.Value != attribute.Description)
                    continue;

                channelId = channel.Key;
                break;
            }

            return channelId != 0;
        }

        public void AddFriend(uint charId)
        {
            Send(new FriendStatusMessage
            {
                Id = charId
            });
        }

        public void RemoveFriend(uint charId)
        {
            Send(new FriendRemoveMessage
            {
                Id = charId
            });
        }

        public void RequestCharacterId(string name)
        {
            Send(new LookupMessage
            {
                Name = name
            });
        }

        public void InvitePrivateGroup(uint recipient)
        {
            Send(new PrivateGroupInviteMessage
            {
                Sender = recipient
            });
        }

        public void AcceptPrivateGroupInvite(uint recipient)
        {
            Send(new PrivateGroupInviteAcceptMessage
            {
                Sender = recipient
            });
        }

        public void SendPrivateGroupMessage(uint channelId, string msg)
        {
            Send(new PrivateGroupMessage
            {
                ChannelId = channelId,
                Sender = CharId,
                Text = msg,
            });
        }

        public void SendPrivateMessage(string recipient, string message, bool logMessage = true)
        {
            if (!NameToIdMap.TryGetValue(recipient, out uint charId))
            {
                RequestCharacterId(recipient);

                _outboundPrivMessageQueue.Enqueue(new PendingPrivateMessage
                {
                    RecipientName = recipient,
                    Message = message,
                    LogMessage = logMessage
                });
            } 
            else
            {
                SendPrivateMessage(charId, message, logMessage);
            }
        }

        public void SendPrivateMessage(int recipient, string message, bool logMessage = true)
        {
            SendPrivateMessage((uint)recipient, message, logMessage);
        }

        public void SendPrivateMessage(uint recipient, string message, bool logMessage = true)
        {
            _outboundPrivMessageQueue.Enqueue(new PendingPrivateMessage
            {
                RecipientId = recipient,
                Message = message,
                LogMessage = logMessage
            });
        }

        private void SendPrivateMessage(PendingPrivateMessage msg)
        {
            Send(new PrivateMsgMessage()
            {
                Sender = msg.RecipientId.Value,
                Text = msg.Message,
                Unk1 = 0x0001,
            });

            if (msg.LogMessage)
                _logger.Information(msg.Message);

            AvailableMessageCount--;
        }

        public void SendChannelMessage(ChannelType channel, string message, bool logMessage = true)
        {
            if (!TryGetChannelId(channel, out int channelId))
            {
                _logger.Error("Could not obtain channel id.");
                return;
            }

            GroupMessageType msgType = GroupMessageType.OOC;

            switch (channel)
            {
                case ChannelType.OTOOC:
                case ChannelType.OTOOCGerman:
                case ChannelType.ClanOOC:
                case ChannelType.ClanOOCGerman:
                case ChannelType.NeuOOC:
                case ChannelType.NeuOOCGerman:
                    msgType = GroupMessageType.OOC;
                    break;

                case ChannelType.OTShopping1150:
                case ChannelType.OTShopping50100:
                case ChannelType.OTShopping100Plus:
                case ChannelType.ClanShopping1150:
                case ChannelType.ClanShopping50100:
                case ChannelType.ClanShopping100Plus:
                case ChannelType.NeuShopping1150:
                case ChannelType.NeuShopping50100:
                case ChannelType.NeuShopping100Plus:
                    msgType = GroupMessageType.Shopping;
                    break;
            }

            Send(new GroupMsgMessage()
            {
                MessageType = msgType,
                ChannelId = channelId,
                Text = message,
            });

            if (logMessage)
                _logger.Information(message);
        }

        protected override void RegisterChatMessageHandlers()
        {
            _chatMsgCallbacks = new Dictionary<ChatMessageType, Action<ChatMessageBody>>();

            _chatMsgCallbacks.Add(ChatMessageType.ServerSalt, (msg) =>
            {
                Send(new ChatLoginRequestMessage
                {
                    Username = Credentials.Username,
                    Credentials = LoginEncryption.MakeChallengeResponse(Credentials, ((ChatServerSaltMessage)msg).ServerSalt)
                });
            });

            _chatMsgCallbacks.Add(ChatMessageType.CharacterList, (msg) =>
            {
                ChatCharacterListMessage charListMsg = (ChatCharacterListMessage)msg;

                ChatCharacter desiredChar = charListMsg.Characters.FirstOrDefault(x => x.Name == Character);

                if (desiredChar == null)
                {
                    _logger.Error($"Could not locate character with name: {Character}.");

                    _logger.Error("Characters on this account:");

                    foreach (ChatCharacter charInfo in charListMsg.Characters)
                        _logger.Error($"\t{charInfo.Name}");

                    return;
                }

                _logger.Debug($"Logging in {desiredChar.Name}:{desiredChar.Id}");

                Send(new ChatSelectCharacterMessage
                {
                    CharacterId = desiredChar.Id
                });

                CharId = desiredChar.Id;
            });

            _chatMsgCallbacks.Add(ChatMessageType.LoginOK, (msg) =>
            {
                _stateMachine.Fire(Trigger.LoginOK);
            });

            _chatMsgCallbacks.Add(ChatMessageType.LoginError, (msg) =>
            {
                ChatLoginErrorMessage loginErrorMsg = (ChatLoginErrorMessage)msg;

                _logger.Error($"Failed to login.");
                //_logger.Error($"Failed to login: {loginErrorMsg.Message}");
            });

            _chatMsgCallbacks.Add(ChatMessageType.FriendStatus, (msg) =>
            {
                FriendStatusMessage friendStatusMsg = (FriendStatusMessage)msg;

                //_logger.Information($"{friendStatusMsg.Id} is now {(friendStatusMsg.Online ? "Online" : "Offline")}");
                FriendStatuses[friendStatusMsg.Id] = friendStatusMsg.Online;
            });

            _chatMsgCallbacks.Add(ChatMessageType.CharacterName, (msg) =>
            {
                CharacterNameMessage charNameMsg = (CharacterNameMessage)msg;

                IdToNameMap[charNameMsg.Id] = charNameMsg.Name;
                NameToIdMap[charNameMsg.Name] = charNameMsg.Id;
            });

            _chatMsgCallbacks.Add(ChatMessageType.LookupMessage, (msg) =>
            {
                LookupMessage lookupMsg = (LookupMessage)msg;

                IdToNameMap[lookupMsg.Id] = lookupMsg.Name;
                NameToIdMap[lookupMsg.Name] = lookupMsg.Id;
            });

            _chatMsgCallbacks.Add(ChatMessageType.PrivateGroupMessage, (msg) =>
            {
                PrivateGroupMessage privateMsg = (PrivateGroupMessage)msg;

                PrivateGroupMessageReceived?.Invoke(this, new PrivateGroupMsg
                {
                    ChannelId = privateMsg.ChannelId,
                    SenderId = privateMsg.Sender,
                    ChannelName = IdToNameMap.ContainsKey(privateMsg.ChannelId) ? IdToNameMap[privateMsg.ChannelId] : "<Unknown>",
                    SenderName = IdToNameMap.ContainsKey(privateMsg.Sender) ? IdToNameMap[privateMsg.Sender] : "<Unknown>",
                    Message = privateMsg.Text
                });
            });

            _chatMsgCallbacks.Add(ChatMessageType.PrivateGroupInvite, (msg) =>
            {
                PrivateGroupInviteMessage privateGroupInviteMsg = (PrivateGroupInviteMessage)msg;

                PrivateGroupInviteMessageReceived?.Invoke(this, new PrivateGroupInviteArgs(privateGroupInviteMsg.Sender));
            });


            _chatMsgCallbacks.Add(ChatMessageType.PrivateMessage, (msg) =>
            {
                PrivateMsgMessage privateMsg = (PrivateMsgMessage)msg;

                PrivateMessageReceived?.Invoke(this, new PrivateMessage
                {
                    SenderId = privateMsg.Sender,
                    SenderName = IdToNameMap.ContainsKey(privateMsg.Sender) ? IdToNameMap[privateMsg.Sender] : "<Unknown>",
                    Message = privateMsg.Text
                });
            });

            _chatMsgCallbacks.Add(ChatMessageType.VicinityMessage, (msg) =>
            {
                VicinityMessage privateMsg = (VicinityMessage)msg;

                VicinityMessageReceived?.Invoke(this, new VicinityMsg
                {
                    SenderId = privateMsg.Sender,
                    SenderName = IdToNameMap.ContainsKey(privateMsg.Sender) ? IdToNameMap[privateMsg.Sender] : "<Unknown>",
                    Message = privateMsg.Text
                });
            });

            _chatMsgCallbacks.Add(ChatMessageType.ChannelList, (msg) =>
            {
                ChannelListMessage channelMsg = (ChannelListMessage)msg;
                ChannelToIdMap[channelMsg.ChannelId] = channelMsg.ChannelName;
            });

            _chatMsgCallbacks.Add(ChatMessageType.GroupMessage, (msg) =>
            {
                GroupMsgMessage groupMsg = (GroupMsgMessage)msg;

                GroupMessageReceived?.Invoke(this, new GroupMsg
                {
                    SenderId = groupMsg.SenderId,
                    SenderName = IdToNameMap.ContainsKey(groupMsg.SenderId) ? IdToNameMap[groupMsg.SenderId] : "<Unknown>",
                    ChannelId = groupMsg.ChannelId,
                    ChannelName = ChannelToIdMap.ContainsKey(groupMsg.ChannelId) ? ChannelToIdMap[groupMsg.ChannelId] : "<Unknown>",
                    Message = groupMsg.Text
                });
            });
        }
    }

    public class PrivateGroupInviteArgs : EventArgs
    {
        public uint Requester { get; }

        public PrivateGroupInviteArgs(uint requester)
        {
            Requester = requester;
        }

        //public void Accept()
        //{
        //}

        //public void Decline()
        //{
        //}
    }

    public class PrivateGroupMsg
    {
        public uint ChannelId;
        public string ChannelName;
        public uint SenderId;
        public string SenderName;
        public string Message;
    }

    public class PendingPrivateMessage
    {
        public uint? RecipientId;
        public string RecipientName;
        public string Message;
        public bool LogMessage;
    }

    public class PrivateMessage
    {
        public uint SenderId;
        public string SenderName;
        public string Message;
    }

    public class VicinityMsg
    {
        public uint SenderId;
        public string SenderName;
        public string Message;
    }

    public class GroupMsg
    {
        public uint SenderId;
        public string SenderName;
        public int ChannelId;
        public string ChannelName;
        public string Message;
    }

    public enum ChannelType
    {
        [Description("OT shopping 11-50")]
        OTShopping1150,
        [Description("OT shopping 50-100")]
        OTShopping50100,
        [Description("OT shopping 100+")]
        OTShopping100Plus,
        [Description("Clan shopping 11-50")]
        ClanShopping1150,
        [Description("Clan shopping 50-100")]
        ClanShopping50100,
        [Description("Clan shopping 100+")]
        ClanShopping100Plus,
        [Description("OT OOC")]
        OTOOC,
        [Description("OT German OOC")]
        OTOOCGerman,
        [Description("Clan OOC")]
        ClanOOC,
        [Description("Clan German OOC")]
        ClanOOCGerman,
        [Description("Neu. OOC")]
        NeuOOC,
        [Description("Neu. German OOC")]
        NeuOOCGerman,
        [Description("Neu. shopping 11-50")]
        NeuShopping1150,
        [Description("Neu. shopping 50-100")]
        NeuShopping50100,
        [Description("Neu. shopping 100+")]
        NeuShopping100Plus,
    }
}
