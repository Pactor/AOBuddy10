using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;

namespace AOSharp.Clientless
{
    public class Organization
    {
        public static EventHandler<OrgInviteResponseEventArgs> OrgInviteResponse;

        internal static void OnOrgServerMessage(OrgServerMessage orgServerMessage)
        {
            if (orgServerMessage.Identity.Instance != Client.LocalDynelId)
                return;

            switch (orgServerMessage.OrgServerMessageType)
            {
                case OrgServerMessageType.OrgInvite:
                    OrgInviteResponse?.Invoke(null, new OrgInviteResponseEventArgs(orgServerMessage.Organization.Instance));
                    break;
            }
        }

        //public static void Promote(uint charInstance)
        //{
        //    SendOrgClientMessage(new Identity(IdentityType.SimpleChar, (int)charInstance), OrgClientCommand.Promote);
        //}

        //public static void Promote(Identity identity)
        //{
        //    SendOrgClientMessage(identity, OrgClientCommand.Promote);
        //}

        //public static void Promote(SimpleChar simpleChar)
        //{
        //    SendOrgClientMessage(simpleChar.Identity, OrgClientCommand.Promote);
        //}

        public static void Invite(uint charInstance)
        {
            SendOrgClientMessage(new Identity(IdentityType.SimpleChar, (int)charInstance), OrgClientCommand.Invite);
        }

        public static void Invite(Identity identity)
        {
            SendOrgClientMessage(identity, OrgClientCommand.Invite);
        }

        public static void Invite(SimpleChar simpleChar)
        {
            SendOrgClientMessage(simpleChar.Identity, OrgClientCommand.Invite);
        }

        public static void Leave()
        {
            if (DynelManager.LocalPlayer.OrgId == 0)
                return;

            Client.Send(new OrgClientMessage
            {
                Command = OrgClientCommand.Leave,
            });
        }

        public static void BankRemove(int amount)
        {
            if (DynelManager.LocalPlayer.OrgId == 0)
                return;

            Client.Send(new OrgClientMessage
            {
                Command = OrgClientCommand.BankRemove,
                Target = DynelManager.LocalPlayer.Identity,
                Unknown = 0,
                Unknown1 = 4,
                IOrgClientMessage = new OrgClientCommandArgsMessage
                {
                    CommandArgs = amount.ToString()
                }
            });
        }

        public static void BankAdd(int amount)
        {
            if (DynelManager.LocalPlayer.OrgId == 0)
                return;

            Client.Send(new OrgClientMessage
            {
                Command = OrgClientCommand.BankAdd,
                Target = DynelManager.LocalPlayer.Identity,
                Unknown = 0,
                Unknown1 = 4,
                IOrgClientMessage = new OrgClientCommandArgsMessage
                {
                    CommandArgs = amount.ToString()
                }
            });
        }

        public static void Kick(SimpleChar target)
        {
            Kick(target.Identity, target.Name);
        }

        public static void Kick(Identity identity, string name)
        {
            Client.Send(new OrgClientMessage
            {
                Command = OrgClientCommand.Kick,
                Target = identity,
                Unknown = 0,
                Unknown1 = 4,
                IOrgClientMessage = new OrgClientCommandArgsMessage
                {
                    CommandArgs = name
                }
            });
        }

        private static void SendOrgClientMessage(Identity identity, OrgClientCommand cmd)
        {
            //Eventually add rank checks

            if (DynelManager.LocalPlayer.OrgId == 0)
                return;

            Client.Send(new OrgClientMessage
            {
                Command = cmd,
                Target = identity,
                Unknown1 = 1,
                Unknown = 0
            });
        }
    }

    public class OrgInviteResponseEventArgs : EventArgs
    {
        public int OrgId { get; }

        public OrgInviteResponseEventArgs(int orgId)
        {
            OrgId = orgId;
        }
    }
}