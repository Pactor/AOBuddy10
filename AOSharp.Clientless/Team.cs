using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Clientless
{
    public static class Team
    {
        public static bool IsInTeam => DynelManager.LocalPlayer.GetStat(Stat.Team) != 0;

        public static EventHandler<TeamRequestEventArgs> TeamRequest;

        public static EventHandler<TeamMemberEventsArgs> TeamMember;

        public static EventHandler<TeamMemberLeftEventsArgs> TeamMemberLeft;

        public static EventHandler<TeamRequestResponseEventsArgs> TeamRequestResponse;

        public static List<TeamMember> Members = new List<TeamMember>();

        public static void Kick(TeamMember teamMember)
        {
            Kick(teamMember.Identity);
        }

        public static void Kick(Identity identity)
        {
            Client.Send(new CharacterActionMessage()
            {
                Action = CharacterActionType.TeamKickMember,
                Target = identity
            });
        }

        public static void Accept(Identity identity)
        {
            Client.Send(new CharacterActionMessage()
            {
                Action = CharacterActionType.TeamRequestReply,
                Target = identity,
                Parameter2 = (int)TeamRequestResponseAction.Accept
            });
        }

        public static void Decline(Identity identity)
        {
            Client.Send(new CharacterActionMessage()
            {
                Action = CharacterActionType.TeamRequestResponse,
                Target = identity,
                Parameter2 = (int)TeamRequestResponseAction.Decline
            });
        }

        public static void TransferLeader(Identity identity)
        {
            Client.Send(new CharacterActionMessage()
            {
                Action = CharacterActionType.TransferLeader,
                Target = identity,
            });
        }

        public static void LeaveTeam()
        {
            Client.Send(new CharacterActionMessage
            {
                Action = CharacterActionType.LeaveTeam
            });
        }

        public static void Invite(SimpleChar player)
        {
            Invite(player.Identity);
        }

        public static void Invite(Identity identity)
        {
            //Client.Send(new CharacterActionMessage
            //{
            //    Action = CharacterActionType.TeamRequest,
            //    Identity = new Identity(IdentityType.SimpleChar, Client.LocalDynelId),
            //    Target = identity
            //});

            Client.Send(new CharacterActionMessage
            {
                Action = CharacterActionType.TeamRequest,
                Identity = new Identity(IdentityType.SimpleChar, Client.LocalDynelId),
                Target = identity,
                Parameter2 = 1,
            });
        }

        public static void Disband()
        {
            foreach (TeamMember member in Members)
            {
                if (member.Identity == DynelManager.LocalPlayer.Identity)
                    continue;

                Kick(member.Identity);
            }
        }

        internal static void OnTeamMessage(CharacterActionMessage teamMessage)
        {
            switch (teamMessage.Action)
            {
                case CharacterActionType.TeamRequestInvite:
                    TeamRequestEventArgs teamReqArgs = new TeamRequestEventArgs(teamMessage.Target);
                    TeamRequest?.Invoke(null, teamReqArgs);
                    break;
                case CharacterActionType.TeamMemberLeft:
                    if (DynelManager.Find(teamMessage.Target, out SimpleChar simpleChar))
                        simpleChar.OnTeamLeft();
                    Identity target = teamMessage.Target;
                    if (target == DynelManager.LocalPlayer.Identity)
                    {
                        Members.Clear();
                    }
                    else
                    {
                        RemoveTeamMember(target);
                    }

                    TeamMemberLeftEventsArgs teamMemberLeftArgs = new TeamMemberLeftEventsArgs(target);
                    TeamMemberLeft?.Invoke(null, teamMemberLeftArgs);
                    break;
                case (CharacterActionType)0x15:
                    TeamRequestResponseEventsArgs teamMemberReplyArgs = new TeamRequestResponseEventsArgs(teamMessage.Target,
                        teamMessage.Parameter2 == 0x14 ? TeamReplyResponse.Declined : TeamReplyResponse.Accepted);
                    TeamRequestResponse?.Invoke(null, teamMemberReplyArgs);
                    break;
                default:
                    break;
            }
        }

        internal static void OnTeamMember(Identity identity, int level, string name)
        {
            if (!TryFindMember(identity, out TeamMember teamMember))
            {
                teamMember = new TeamMember
                {
                    Identity = identity,
                    Level = level,
                    Name = name
                };

                Members.Add(teamMember);
            }

            TeamMemberEventsArgs teamMemberArgs = new TeamMemberEventsArgs(identity);
            TeamMember?.Invoke(null, teamMemberArgs);
        }

        internal static void RemoveTeamMember(Identity identity)
        {
            if (!TryFindMember(identity, out TeamMember teamMember))
                return;

            Members.Remove(teamMember);
        }

        private static bool TryFindMember(Identity identity, out TeamMember teamMember)
        {
            teamMember = Members.FirstOrDefault(x => x.Identity == identity);
            return teamMember != null;
        }
    }

    public class TeamRequestEventArgs : EventArgs
    {
        public Identity Requester { get; }

        public TeamRequestEventArgs(Identity requester)
        {
            Requester = requester;
        }

        public void Accept()
        {
            Team.Accept(Requester);
        }

        public void Decline()
        {
            Team.Decline(Requester);
        }
    }

    public class TeamMemberEventsArgs : EventArgs
    {
        public Identity Identity { get; }

        public TeamMemberEventsArgs(Identity identity)
        {
            Identity = identity;
        }
    }

    public class TeamMemberLeftEventsArgs : EventArgs
    {
        public Identity Identity { get; }

        public TeamMemberLeftEventsArgs(Identity identity)
        {
            Identity = identity;
        }
    }

    public class TeamRequestResponseEventsArgs : EventArgs
    {
        public Identity Identity { get; }

        public TeamReplyResponse Response { get; }

        public TeamRequestResponseEventsArgs(Identity identity, TeamReplyResponse response)
        {
            Response = response;
            Identity = identity;
        }
    }

    public enum TeamReplyResponse
    {
        Accepted,
        Declined
    }
}