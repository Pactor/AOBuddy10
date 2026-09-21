namespace SmokeLounge.AOtomation.Messaging.GameData
{
    public enum TradeAction : byte
    {
        Open = 0,
        Accept = 1,
        Decline = 2,
        Confirm = 3,
        Complete = 4,
        AddItem = 5,
        RemoveItem = 6,
        UpdateCredits = 7,
        OtherPlayerAddItem = 8
    }

    public enum KnuBotTradeAction
    {
        AddItem = 0,
        RemoveItem = 1,
    }
}
