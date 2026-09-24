using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOBuddy
{
    /// <summary>
    /// The wire commands every system sends by hand, in ONE place (R1.6) — so a fix to how a command
    /// is shaped reaches every sender at once instead of the eight hand-rolled copies that used to
    /// live in the walkers. Travel objects (mission floor buttons, terminals, grid, whompas) are not
    /// tracked as findable dynels, so a lookup-based Dynel.Use() never fires for them: the exact
    /// identity captured from the owner's use is commanded directly.
    /// </summary>
    public static class GameCommands
    {
        /// <summary>
        /// GenericCmd Use on a WORLD object (floor button, lift, terminal, grid, whompa, bank, vendor
        /// machine): Count=1, Temp4=1 — the exact bytes the owner's client sends (USE-TRAVEL notes;
        /// capture 20260923-201746).
        /// </summary>
        public static void UseObject(LocalPlayer me, Identity target)
        {
            Client.Send(new GenericCmdMessage { Action = GenericCmdAction.Use, User = me.Identity, Target = target, Count = 1, Temp4 = 1 });
        }

        /// <summary>
        /// GenericCmd Use in its OPEN form (Count=1, Temp4=0): a bag in the inventory, or a container
        /// holding a mission item — opening it, not activating it.
        /// </summary>
        public static void OpenContainer(LocalPlayer me, Identity target)
        {
            Client.Send(new GenericCmdMessage { Action = GenericCmdAction.Use, User = me.Identity, Target = target, Count = 1, Temp4 = 0 });
        }
    }
}