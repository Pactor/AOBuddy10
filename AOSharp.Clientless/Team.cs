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

        /// <summary>Raised when a member's health or nano changes (TeamMemberInfoMessage).</summary>
        public static EventHandler<TeamMemberEventsArgs> TeamMemberVitals;

        internal static void OnTeamMember(Identity identity, int level, string name, short profession, int raidGroup)
        {
            if (!TryFindMember(identity, out TeamMember teamMember))
            {
                teamMember = new TeamMember { Identity = identity };
                Members.Add(teamMember);
            }

            // Re-sent whenever the member's row changes (a ding, a raid regroup), so update in place
            // rather than only filling a new row - otherwise a levelling teammate stays at his old level.
            teamMember.Level = level;
            teamMember.Name = name;
            teamMember.Profession = profession;
            teamMember.RaidGroup = raidGroup;

            TeamMemberEventsArgs teamMemberArgs = new TeamMemberEventsArgs(identity);
            TeamMember?.Invoke(null, teamMemberArgs);
        }

        /// <summary>
        /// The team window's live vitals for one member. The server sends this for members in our own
        /// playfield whenever their health or nano moves, whether or not they are in render range, so
        /// it is a truer read on a teammate than his dynel's stats - those go stale the moment he is
        /// no longer being broadcast to us.
        /// </summary>
        internal static void OnTeamMemberInfo(Identity identity, int currentHealth, int maxHealth, int currentNano, int maxNano)
        {
            if (!TryFindMember(identity, out TeamMember teamMember))
            {
                // Vitals can arrive before the row itself; keep them rather than dropping them.
                teamMember = new TeamMember { Identity = identity };
                Members.Add(teamMember);
            }

            teamMember.CurrentHealth = currentHealth;
            teamMember.MaxHealth = maxHealth;
            teamMember.CurrentNano = currentNano;
            teamMember.MaxNano = maxNano;
            teamMember.VitalsUpdated = DateTime.UtcNow;

            TeamMemberVitals?.Invoke(null, new TeamMemberEventsArgs(identity));
        }

        /// <summary>The team row for this character, or null when he is not on our team.</summary>
        public static TeamMember Find(Identity identity)
        {
            TryFindMember(identity, out TeamMember teamMember);
            return teamMember;
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