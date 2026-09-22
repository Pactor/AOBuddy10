using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using AOSharp.Clientless.Net;
using SmokeLounge.AOtomation.Messaging.Messages;
using AOSharp.Clientless.Common;
using AOSharp.Clientless.Chat;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.GameData;
using AOSharp.Common.SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.Messages.SystemMessages;
using System.Threading.Tasks;
using Stateless;

namespace AOSharp.Clientless
{
    public class ClientConfig
    {
        public bool AutoReconnect = true;
        public int ReconnectDelay = 30000; //TODO: implement exponential backoff
    }

    public static class Client
    {
        internal static Credentials Credentials;
        public static string CharacterName { get; internal set; }
        public static Dimension Dimension { get; internal set; }
        public static int LocalDynelId { get; internal set; }
        public static int OrgId { get; internal set; }
        public static string OrgName { get; internal set; }
        public static int ServerId { get; internal set; }
        public static ClientConfig Config = new ClientConfig();

        public static bool InPlay => _netSession.InPlay;
        public static bool Connected => _netSession.Connected;

        private static NetworkSession _netSession;
        private static UpdateLoop _updateLoop;
        private static bool _isFirstPlayshift = true;

        internal static Logger Logger;
        internal static HostProxy HostProxy;
        internal static bool LogDeserializationErrors = false;
        internal static bool ItemDataLoaded = true;

        public static ChatClient Chat;

        public static EventHandler<double> OnUpdate;
        public static EventHandler<Message> MessageReceived;
        public static EventHandler<byte[]> PacketReceived;
        public static Action<CharacterSelect> CharacterSelect;
        public static Action<bool> CharacterInPlay;
        public static Action Died;
        public static Action Disconnected;

        // Pet lifecycle, straight from the server (AddPet / RemovePet / PetToMaster). PetAttached
        // also states what the pet is for; the same value is on the pet's own character update as
        // NpcChar.PetTypeId, so either source works and they agree.
        public static Action<Identity> PetAdded;
        public static Action<Identity> PetRemoved;
        public static Action<Identity, PetType> PetAttached;

        private static Dictionary<SystemMessageType, Action<SystemMessage>> _sysMsgCallbacks;
        private static Dictionary<N3MessageType, Action<N3Message>> _n3MsgCallbacks;

        public static ClientDomain CreateInstance(string username, string password, string characterName, Dimension dimension, Logger logger)
        {
            //TODO: Validate params

            return ClientDomain.CreateDomain(username, password, characterName, dimension, logger);
        }

        public static void UseCurrentDomain(string username, string password, string characterName, Dimension dimension, Logger logger, bool useBuiltInLooper = true, bool useChat = true)
        {
            Credentials = new Credentials(username, password);
            CharacterName = characterName;
            Dimension = dimension;
            Logger = logger;

            if(useChat)
                CreateChatClient();

            Init(useBuiltInLooper);
        }


        public static void Send(MessageBody msgBody) => _netSession.Send(msgBody);

        public static void Send(Message message) => _netSession.Send(message);

        public static void Send(ChatMessageBody msgBody) => Chat.Send(msgBody);

        public static void SendPrivateMessage(int charId, string message, bool logMessage = true) => SendPrivateMessage((uint)charId, message, logMessage);

        public static void SendPrivateMessage(uint charId, string message, bool logMessage = true)
        {
            if (Chat == null)
                return;

            Chat.SendPrivateMessage(charId, message, logMessage);
        }

        public static void SendTeamMessage(string message, bool logMessage = true)
        {
            if (!DynelManager.LocalPlayer.TryGetStat(Stat.Team, out int teamId) || teamId == 0)
            {
                Logger.Error("Team channel id is not valid.");
                return;
            }

            Send(new GroupMsgMessage()
            {
                MessageType = GroupMessageType.Team,
                ChannelId = teamId,
                Text = message,
            });

            if (logMessage)
                Logger.Information(message);
        }

        public static void SendChannelMessage(ChannelType channel, string message, bool logMessage = true)
        {
            if (Chat == null)
                return;

            Chat.SendChannelMessage(channel, message, logMessage);
        }

        public static void SendOrgMessage(string message, bool logMessage = true)
        {
            if (!DynelManager.LocalPlayer.TryGetStat(Stat.Clan, out int clanId) || clanId == 0)
            {
                Logger.Error("Could not obtain LocalPlayer org stat.");
                return;
            }

            Send(new GroupMsgMessage()
            {
                MessageType = GroupMessageType.Org,
                ChannelId = clanId,
                Text = message,
            });

            if (logMessage)
                Logger.Information(message);
        }

        public static void InfoRequest(Identity identity)
        {
            Send(new CharacterActionMessage()
            {
                Action = CharacterActionType.InfoRequest,
                Identity = DynelManager.LocalPlayer.Identity,
                Target = identity,
            });
        }

        public static void Disconnect()
        {
            Teardown();
        }

        public static void SuppressItemDataLoad(bool shouldSuppress = true)
        {
            ItemDataLoaded = !shouldSuppress;
        }

        public static void SuppressDeserializationErrors()
        {
            LogDeserializationErrors = true;
        }

        internal static void CreateChatClient()
        {
            Chat = new ChatClient(Credentials, CharacterName, Dimension, Logger);
        }

        internal static void Init(bool useBuiltInLooper = true)
        {
            Playfield.LoadPlayfieldNames();

            RegisterSystemMessageHandlers();
            RegisterN3MessageHandlers();

            _netSession = new NetworkSession(Logger, _sysMsgCallbacks, _n3MsgCallbacks);

            if (useBuiltInLooper)
            {
                _updateLoop = new UpdateLoop(Update);
                _updateLoop.Start();
            }

            _netSession.Connect();
            _netSession.NetworkStateChanged += OnNetworkStateTransition;

            Chat?.Init(false);
        }

        internal static void Teardown()
        {
            _updateLoop?.Stop();
            _netSession?.Disconnect();
            Logger.Dispose();
        }

        internal static void Update(double deltaTime)
        {
            _netSession.Update();

            Chat?.Update(deltaTime);
            IPCChannel.UpdateInternal();

            if (InPlay)
                OnUpdate?.Invoke(null, deltaTime);
        }

        internal static void OnTeleportStart()
        {
        }

        internal static void SelectCharacter(int id)
        {
            Send(new SelectCharacterMessage
            {
                CharacterId = id
            });

            LocalDynelId = id;
        }

        private static void OnNetworkStateTransition(StateMachine<State, Trigger>.Transition transition)
        {
            if (transition.Destination == State.Disconnected)
            {
                Disconnected?.Invoke();
                _isFirstPlayshift = true;
            }
        }

        private static void RegisterSystemMessageHandlers()
        {
            _sysMsgCallbacks = new Dictionary<SystemMessageType, Action<SystemMessage>>();

            _sysMsgCallbacks.Add(SystemMessageType.ServerSalt, (msg) =>
            {
                Send(new UserCredentialsMessage
                {
                    UserName = Client.Credentials.Username,
                    Credentials = LoginEncryption.MakeChallengeResponse(Client.Credentials, ((ServerSaltMessage)msg).ServerSalt)
                });
            });

            _sysMsgCallbacks.Add(SystemMessageType.CharacterList, (msg) =>
            {
                CharacterListMessage charListMsg = (CharacterListMessage)msg;

                if (CharacterSelect == null)
                {
                    LoginCharacterInfo desiredChar = charListMsg.Characters.FirstOrDefault(x => x.Name == CharacterName);

                    if (desiredChar == null)
                    {
                        Logger.Error($"Could not locate character with name: {CharacterName}.");

                        Logger.Error("Characters on this account:");

                        foreach (LoginCharacterInfo charInfo in charListMsg.Characters)
                            Logger.Error($"\t{charInfo.Name}");

                        return; //TODO: Trigger fatal error state?
                    }

                    SelectCharacter(desiredChar.Id);
                }
                else
                {
                    CharacterSelect.Invoke(new CharacterSelect
                    {
                        AllowedCharacters = charListMsg.AllowedCharacters,
                        Expansions = (ExpansionFlags)charListMsg.Expansions,
                        Characters = charListMsg.Characters.Select(x => new CharacterSelect.Character
                        {
                            Id = x.Id,
                            Name = x.Name
                        }).ToList(),
                    });
                }
            });
        }

        private static void RegisterN3MessageHandlers()
        {
            _n3MsgCallbacks = new Dictionary<N3MessageType, Action<N3Message>>();

            _n3MsgCallbacks.Add(N3MessageType.FullCharacter, (msg) =>
            {
                FullCharacterMessage fullCharMsg = (FullCharacterMessage)msg;

                // Only OUR OWN FullCharacter updates the local player. The server also sends other characters'
                // FullCharacter once we're near/teamed with them (e.g. the owner) — applying those overwrote
                // our SpellList/stats with theirs, so the bot dumped and cast the OWNER'S nanos (a Keeper's).
                // Always let the FIRST one through (it establishes us, before LocalDynelId may be set); after
                // that, ignore any whose identity isn't ours.
                if (!_isFirstPlayshift && Client.LocalDynelId != 0 && fullCharMsg.Identity.Instance != Client.LocalDynelId)
                    return;

                DynelManager.LocalPlayerProxy.ApplyFullCharUpdate(fullCharMsg);

                // AUTHORITATIVE pet ownership: our own FullCharacter lists our pets (decoded by the corrected
                // fallback reader when a pet is up). Mark those NPCs as owned so me.Pets is reliable regardless
                // of the flaky per-update pet-master bit. Match on INSTANCE — the FullCharacter pet-list
                // identity uses a different Type than the pet's own dynel identity.
                if (fullCharMsg.Pets != null && fullCharMsg.Pets.Length > 0)
                {
                    LocalPlayer me = DynelManager.LocalPlayer;
                    if (me != null)
                        foreach (Identity petId in fullCharMsg.Pets)
                            foreach (NpcChar npc in DynelManager.Npcs)
                                if (npc.Identity.Instance == petId.Instance)
                                    npc.Owner = me.Identity;
                }

                Send(new CharInPlayMessage());
                CharacterInPlay?.Invoke(_isFirstPlayshift);

                if (_isFirstPlayshift)
                    _isFirstPlayshift = false;
            });

            _n3MsgCallbacks.Add(N3MessageType.ContainerAddItem, (msg) =>
            {
                ContainerAddItem contAddItem = (ContainerAddItem)msg;

                if (contAddItem.Identity == DynelManager.LocalPlayer.Identity)
                    Inventory.OnContainerAddItem(contAddItem.Source, contAddItem.Target, contAddItem.Slot);
            });

            _n3MsgCallbacks.Add(N3MessageType.Bank, (msg) =>
            {
                BankMessage bankMsg = (BankMessage)msg;

                if (bankMsg.Identity == DynelManager.LocalPlayer.Identity)
                    Inventory.OnBankUpdate(bankMsg);
            });

            _n3MsgCallbacks.Add(N3MessageType.InventoryUpdate, (msg) =>
            {
                InventoryUpdateMessage invMsg = (InventoryUpdateMessage)msg;
                Inventory.OnContainerUpdate(invMsg.InventoryIdentity, invMsg.Items, invMsg.Handle);
            });

            _n3MsgCallbacks.Add(N3MessageType.SimpleItemFullUpdate, (msg) =>
            {
                SimpleItemFullUpdateMessage sifu = (SimpleItemFullUpdateMessage)msg;
                FullUpdateProxy.OnSIFU(sifu);
            });

            _n3MsgCallbacks.Add(N3MessageType.WeaponItemFullUpdate, (msg) =>
            {
                WeaponItemFullUpdateMessage wifu = (WeaponItemFullUpdateMessage)msg;
                FullUpdateProxy.OnWIFU(wifu);
            });

            _n3MsgCallbacks.Add(N3MessageType.ChestFullUpdate, (msg) =>
            {
                ChestFullUpdateMessage cfu = (ChestFullUpdateMessage)msg;
                FullUpdateProxy.OnCFU(cfu);
            });

            _n3MsgCallbacks.Add(N3MessageType.SimpleCharFullUpdate, (msg) =>
            {
                SimpleCharFullUpdateMessage simpleCharFullUpdateMsg = (SimpleCharFullUpdateMessage)msg;
                DynelManager.OnDynelSpawned(simpleCharFullUpdateMsg);
            });

            _n3MsgCallbacks.Add(N3MessageType.OrgInfoPacket, (msg) =>
            {
                OrgInfoPacketMessage orgInfoPacketMessage = (OrgInfoPacketMessage)msg;
                DynelManager.OnOrgInfoPacket(orgInfoPacketMessage);
            });

            _n3MsgCallbacks.Add(N3MessageType.OrgServer, (msg) =>
            {
                OrgServerMessage orgServerMessage = (OrgServerMessage)msg;
                Organization.OnOrgServerMessage(orgServerMessage);
            });

            _n3MsgCallbacks.Add(N3MessageType.VendingMachineFullUpdate, (msg) =>
            {
                VendingMachineFullUpdateMessage vendingMachineFullUpdateMsg = (VendingMachineFullUpdateMessage)msg;

                if (vendingMachineFullUpdateMsg.Position != null)
                    DynelManager.OnDynelSpawned(vendingMachineFullUpdateMsg);
            });

            _n3MsgCallbacks.Add(N3MessageType.Despawn, (msg) =>
            {
                DespawnMessage despawnMessage = (DespawnMessage)msg;
                DynelManager.OnDynelDespawned(despawnMessage.Identity);
            });

            // DIAGNOSTIC ONLY: log the server's SetPos for the LOCAL player so we can compare the
            // server's idea of the bot's position against where the bot thinks it is (desync check).
            // This does NOT move the bot and does NOT apply to any dynel — it only fires the observe
            // event that the plugin logs. It is safe: setpos is never applied (that is what warps).
            _n3MsgCallbacks.Add(N3MessageType.SetPos, (msg) =>
            {
                SetPosMessage setPos = (SetPosMessage)msg;
                DynelManager.OnServerSetPos(setPos.Identity, setPos.Position);
            });

            _n3MsgCallbacks.Add(N3MessageType.PlayfieldAnarchyF, (msg) =>
            {
                PlayfieldAnarchyFMessage playfieldMessage = (PlayfieldAnarchyFMessage)msg;
                Playfield.Init(playfieldMessage);
            });

            _n3MsgCallbacks.Add(N3MessageType.PlayfieldAllTowers, (msg) =>
            {
                PlayfieldAllTowersMessage playfieldAllTowersMessage = (PlayfieldAllTowersMessage)msg;

                foreach (TowerInfo tower in playfieldAllTowersMessage.TowerInfo)
                    Playfield.MakeTower(tower, PlayfieldTowerUpdateType.InitialLoad);
            });

            _n3MsgCallbacks.Add(N3MessageType.PlayfieldTowerUpdateClient, (msg) =>
            {
                PlayfieldTowerUpdateClientMessage playfieldTowerUpdateClientMessage = (PlayfieldTowerUpdateClientMessage)msg;

                if (playfieldTowerUpdateClientMessage.UpdateType == PlayfieldUpdateClientType.Planted)
                    Playfield.MakeTower(playfieldTowerUpdateClientMessage.Tower, PlayfieldTowerUpdateType.Planted);
                else
                    Playfield.DestroyTower(playfieldTowerUpdateClientMessage.TowerId);
            });

            _n3MsgCallbacks.Add(N3MessageType.CharDCMove, (msg) =>
            {
                CharDCMoveMessage moveMessage = (CharDCMoveMessage)msg;

                DynelManager.OnDynelMovementChanged(moveMessage.Identity, moveMessage.Position, moveMessage.Heading, moveMessage.MoveType);
            });

            _n3MsgCallbacks.Add(N3MessageType.CharacterAction, (msg) =>
            {
                CharacterActionMessage charActionMessage = (CharacterActionMessage)msg;
                OnCharacterAction(charActionMessage);
            });

            _n3MsgCallbacks.Add(N3MessageType.Stat, (msg) =>
            {
                StatMessage statMsg = (StatMessage)msg;

                if (DynelManager.Find(statMsg.Identity, out Dynel statTarget))
                {
                    foreach (var stat in statMsg.Stats)
                        statTarget.SetStat(stat.Value1, (int)stat.Value2);
                }
            });

            // Keep current HP live. The server rarely re-pushes Stat.Health, but every point of
            // damage or healing arrives as a HealthDamage packet whose Health field is the HP the
            // character has LEFT after the change. Without this, tracked HP goes stale after a
            // recharge/heal (bot thinks it's still hurt) — breaking self-heal, owner-heal and rest.
            _n3MsgCallbacks.Add(N3MessageType.HealthDamage, (msg) =>
            {
                HealthDamageMessage hd = (HealthDamageMessage)msg;

                if (DynelManager.Find(hd.Target, out Dynel hpTarget))
                    hpTarget.SetStat(Stat.Health, hd.TargetHp);
            });

            // Keep skills/abilities live. After login these change via SkillMessage (buffs like
            // Composite Attribute, IP spends, equipment) — the SDK had no handler, so skills were
            // frozen at their login values and buffed skills (e.g. First Aid) read low, wrongly
            // failing item/nano use-requirements. Apply each update to the character's stats.
            _n3MsgCallbacks.Add(N3MessageType.Skill, (msg) =>
            {
                SkillMessage skillMsg = (SkillMessage)msg;

                if (skillMsg.Skills != null && DynelManager.Find(skillMsg.Identity, out Dynel skillTarget))
                    foreach (var skill in skillMsg.Skills)
                        skillTarget.SetStat(skill.Value1, (int)skill.Value2);
            });

            // Keep our LEVEL live. Levelling is SERVER-PUSHED as its own NewLevelMessage (not a Stat or
            // Skill update), and the SDK had no handler for it — so Stat.Level stayed frozen at the login
            // value: the character read the old level forever and nothing could notice a "ding". Apply the
            // new level to the character's stats so Level/GetStat(Stat.Level) tracks reality.
            _n3MsgCallbacks.Add(N3MessageType.NewLevel, (msg) =>
            {
                NewLevelMessage lvlMsg = (NewLevelMessage)msg;

                if (DynelManager.Find(lvlMsg.Identity, out Dynel lvlTarget))
                    lvlTarget.SetStat(Stat.Level, lvlMsg.Level);
                else
                    DynelManager.LocalPlayer?.SetStat(Stat.Level, lvlMsg.Level);
            });

            _n3MsgCallbacks.Add(N3MessageType.TeamMember, (msg) =>
            {
                TeamMemberMessage teamMemberMsg = (TeamMemberMessage)msg;

                Team.OnTeamMember(teamMemberMsg.Character, teamMemberMsg.Level, teamMemberMsg.Name,
                                  teamMemberMsg.Profession, teamMemberMsg.RaidGroup);

                if (DynelManager.Find(teamMemberMsg.Identity, out Dynel statTarget))
                {
                    statTarget.SetStat(Stat.Team, teamMemberMsg.Team.Instance);
                }
            });

            // The team window's live health/nano for one member. This was decoded and then dropped
            // on the floor, so a teammate's vitals could only be read off his dynel - which goes
            // stale as soon as he stops being broadcast to us. These keep coming while he is in the
            // playfield at any distance.
            _n3MsgCallbacks.Add(N3MessageType.TeamMemberInfo, (msg) =>
            {
                TeamMemberInfoMessage infoMsg = (TeamMemberInfoMessage)msg;

                Team.OnTeamMemberInfo(infoMsg.Character, infoMsg.CurrentHealth, infoMsg.MaxHealth,
                                      infoMsg.CurrentNano, infoMsg.MaxNano);

                // Mirror onto the dynel when we can see it, so existing stat readers agree.
                if (DynelManager.Find(infoMsg.Character, out Dynel vitalsTarget))
                {
                    vitalsTarget.SetStat(Stat.Health, infoMsg.CurrentHealth);
                    vitalsTarget.SetStat(Stat.MaxHealth, infoMsg.MaxHealth);
                    vitalsTarget.SetStat(Stat.CurrentNano, infoMsg.CurrentNano);
                    vitalsTarget.SetStat(Stat.MaxNanoEnergy, infoMsg.MaxNano);
                }
            });

            // Pet lifecycle. LocalPlayer.Pets still derives the roster from the dynel list, but these
            // are the exact moments a summon landed or a pet was lost, so a caller can react at once
            // instead of noticing on its next poll. PetToMaster's attach also repeats the pet's type.
            _n3MsgCallbacks.Add(N3MessageType.AddPet, (msg) =>
            {
                AddPetMessage addPetMsg = (AddPetMessage)msg;
                PetAdded?.Invoke(addPetMsg.PetIdentity);
            });

            _n3MsgCallbacks.Add(N3MessageType.RemovePet, (msg) =>
            {
                RemovePetMessage removePetMsg = (RemovePetMessage)msg;
                PetRemoved?.Invoke(removePetMsg.PetIdentity);
            });

            _n3MsgCallbacks.Add(N3MessageType.PetToMaster, (msg) =>
            {
                PetToMasterMessage petToMasterMsg = (PetToMasterMessage)msg;

                // Operation 1 is the attach and carries the pet type; operation 2 is the detach.
                if (petToMasterMsg.Operation == 1)
                    PetAttached?.Invoke(petToMasterMsg.PetIdentity, (PetType)petToMasterMsg.AttachNotificationValue);
            });

            _n3MsgCallbacks.Add(N3MessageType.Buff, (msg) =>
            {
                BuffMessage buffMsg = (BuffMessage)msg;
                OnBuffMessage(buffMsg.Identity, buffMsg.Buff.Instance);
            });

            _n3MsgCallbacks.Add(N3MessageType.CastNanoSpell, (msg) =>
            {
                CastNanoSpellMessage castNanoSpellMsg = (CastNanoSpellMessage)msg;
                OnCastNanoSpell(castNanoSpellMsg.Identity, castNanoSpellMsg.TargetPresent);
            });

            _n3MsgCallbacks.Add(N3MessageType.Trade, (msg) =>
            {
                TradeMessage tradeMsg = (TradeMessage)msg;
                Trade.OnTradeMessageReceived(tradeMsg);
            });

            _n3MsgCallbacks.Add(N3MessageType.GenericCmd, (msg) =>
            {
                GenericCmdMessage genericCmdMsg = (GenericCmdMessage)msg;
                DynelManager.OnDynelUsed(genericCmdMsg.User, genericCmdMsg.Target);
            });

            _n3MsgCallbacks.Add(N3MessageType.TemplateAction, (msg) =>
            {
                TemplateActionMessage templateMsg = (TemplateActionMessage)msg;

                if (templateMsg.Identity != DynelManager.LocalPlayer.Identity)
                    return;

                if ((templateMsg.Action == 6 || templateMsg.Action == 85) && templateMsg.Placement == IdentityType.Inventory)
                {
                    Trade.OnTemplateAction(templateMsg.ItemLowId, templateMsg.ItemHighId, templateMsg.Quality);
                }
                else if (templateMsg.Placement == IdentityType.OverflowWindow)
                {
                    Inventory.OnTemplateMessage(templateMsg.ItemLowId, templateMsg.ItemHighId, templateMsg.Quality);
                }
            });

            _n3MsgCallbacks.Add(N3MessageType.AddTemplate, (msg) =>
            {
                AddTemplateMessage tmpMsg = (AddTemplateMessage)msg;
                Inventory.OnAddTemplateMessage(tmpMsg.LowId, tmpMsg.HighId, tmpMsg.Quality);
            });

            _n3MsgCallbacks.Add(N3MessageType.Attack, (msg) =>
            {
                AttackMessage attackMessage = (AttackMessage)msg;
                if (DynelManager.Find(attackMessage.Identity, out SimpleChar attacker))
                    attacker.FightingIdentity = attackMessage.Target;
            });

            _n3MsgCallbacks.Add(N3MessageType.StopFight, (msg) =>
            {
                StopFightMessage stopFightMessage = (StopFightMessage)msg;
                if (DynelManager.Find(stopFightMessage.Identity, out SimpleChar attacker))
                    attacker.FightingIdentity = null;
            });

            _n3MsgCallbacks.Add(N3MessageType.TeamInvite, (msg) =>
            {
                TeamInviteMessage teamInviteMsg = (TeamInviteMessage)msg;

                TeamRequestEventArgs teamReqArgs = new TeamRequestEventArgs(teamInviteMsg.Requestor);
                Team.TeamRequest?.Invoke(null, teamReqArgs);
            });
        }

        private static void OnCharacterAction(CharacterActionMessage charActionMessage)
        {
            switch (charActionMessage.Action)
            {
                case CharacterActionType.TeamRequestInvite:
                case CharacterActionType.TeamRequestReply:
                case CharacterActionType.TeamRequestResponse:
                case CharacterActionType.TeamKickMember:
                case CharacterActionType.TeamMemberLeft:
                case (CharacterActionType)0x15: //TeamRequestResponse
                    Team.OnTeamMessage(charActionMessage);
                    break;
                case CharacterActionType.SetNanoDuration:
                    SetNanoDurationCharAction(charActionMessage.Identity, charActionMessage.Target.Instance, charActionMessage.Parameter2);
                    break;
                case CharacterActionType.SpecialUsed:
                    SpecialUsedAction(charActionMessage.Identity, (Stat)charActionMessage.Parameter1, charActionMessage.Parameter2);
                    break;
                case CharacterActionType.SpecialAvailable:
                    SpecialAvailableAction(charActionMessage.Identity, (Stat)charActionMessage.Parameter2);
                    break;
                case CharacterActionType.FinishNanoCasting:
                case CharacterActionType.InterruptNanoCasting:
                    FinishNanoCastingAction(charActionMessage.Identity);
                    break;
                case CharacterActionType.DeleteItem:
                    DeleteItemAction(charActionMessage.Target);
                    break;
                case (CharacterActionType)0x2F: //Temp item expire
                    //ToDo item deletion on expire
                    break;
                case CharacterActionType.Death:
                    OnCharacterDeath(charActionMessage.Identity);
                    break;
                default:
                    break;
            }
        }

        private static void DeleteItemAction(Identity target)
        {
            switch (target.Type)
            {
                case IdentityType.Backpack:
                    Inventory.RemoveContainerItem(target, out Container container, out Item item);
                    Inventory.ContainerItemRemoved?.Invoke(container, item);
                    break;
                case IdentityType.Inventory:
                    Inventory.RemoveItem(target, out item);
                    Inventory.ItemRemoved?.Invoke(item);
                    break;
                case IdentityType.BankByRef:
                    Inventory.RemoveBankItem(target, out item);
                    Inventory.BankItemRemoved?.Invoke(item);
                    break;
                default:
                    Logger.Warning($"Unhandled delete action identity type {target.Type}. Please report");
                    break;

            }
        }

        private static void FinishNanoCastingAction(Identity identity)
        {
            if (!DynelManager.Find(identity, out SimpleChar simpleChar))
                return;

            if (!(simpleChar is LocalPlayer))
                return;

            DynelManager.LocalPlayer.SetCastState(false);
        }

        private static void OnCastNanoSpell(Identity identity, int unknown1)
        {
            if (unknown1 == 1)
                return;

            if (!DynelManager.Find(identity, out SimpleChar simpleChar))
                return;

            if (!(simpleChar is LocalPlayer))
                return;

            DynelManager.LocalPlayer.SetCastState(true);
        }

        private static void OnCharacterDeath(Identity identity)
        {
            if (identity == DynelManager.LocalPlayer.Identity)
            {
                Logger.Warning($"I'm dead");
                DynelManager.LocalPlayer.StopAttack();
                Died?.Invoke();
                Task.Delay(5000).ContinueWith(t =>
                {
                    Send(new CharacterActionMessage
                    {
                        Action = CharacterActionType.Die
                    });
                });
            }
            else
            {
                if (DynelManager.Find(identity, out SimpleChar character))
                    character.SetStat(Stat.Health, 0);
            }
        }

        private static void SpecialUsedAction(Identity identity, Stat stat, int cooldownTime)
        {
            if (!DynelManager.Find(identity, out SimpleChar simpleChar))
                return;

            if (!(simpleChar is LocalPlayer))
                return;

            DynelManager.LocalPlayer.RegisterCooldown(stat, cooldownTime);
        }

        private static void SpecialAvailableAction(Identity identity, Stat stat)
        {
            if (!DynelManager.Find(identity, out SimpleChar simpleChar))
                return;

            if (!(simpleChar is LocalPlayer))
                return;

            DynelManager.LocalPlayer.RemoveCooldown(stat);
        }

        private static void OnBuffMessage(Identity identity, int nanoId)
        {
            if (!DynelManager.Find(identity, out SimpleChar simpleChar))
                return;

            BuffStatus.OnBuffMessage(simpleChar, nanoId);

            simpleChar.RemoveBuff(nanoId);
        }

        private static void SetNanoDurationCharAction(Identity identity, int nanoId, int param2)
        {
            if (!DynelManager.Find(identity, out SimpleChar simpleChar))
                return;

            if (!simpleChar.Buffs.Find(nanoId, out Buff newBuff))
            {
                newBuff = new Buff(nanoId);
                simpleChar.RegisterBuff(newBuff);
            }

            newBuff.Cooldown.SetExpireTime(param2 / 100f);
        }
    }
}