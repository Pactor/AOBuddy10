using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;

namespace AOSharp.Clientless
{
    public static class PrivateGroupChat
    {
        public static void Join(uint charId)
        {
            Client.Send(new PrivateGroupInviteAcceptMessage()
            {
                Sender = charId
            });
        }
    }
}