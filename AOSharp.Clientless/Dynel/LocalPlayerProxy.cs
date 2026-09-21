using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOSharp.Clientless
{
    public class LocalPlayerProxy
    {
        public static LocalPlayer LocalPlayer;
        public FullCharacterMessage FullCharMsg;

        public void ApplySimpleCharFullUpdate(SimpleCharFullUpdateMessage simpleCharMsg)
        {
            LocalPlayer = new LocalPlayer(simpleCharMsg);

            if (FullCharMsg == null)
                return;

            ApplyChanges(FullCharMsg);
        }

        public void ApplyFullCharUpdate(FullCharacterMessage fullCharMsg)
        {
            if (LocalPlayer == null)
            {
                FullCharMsg = fullCharMsg;
                return;
            }

            ApplyChanges(fullCharMsg);
        }

        private void ApplyChanges(FullCharacterMessage fullCharMsg)
        {
            LocalPlayer.ApplyFullCharacter(fullCharMsg);
            Inventory.OnFullCharacterMessage(fullCharMsg.InventorySlots);
            FullCharMsg = null;
        }
    }
}