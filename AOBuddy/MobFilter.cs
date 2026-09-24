using AOSharp.Clientless;

namespace AOBuddy
{
    /// <summary>
    /// The dynel-flag masks that separate fightable monsters from everything else, in ONE place
    /// (R1.7): a dynel carrying any of these flags is a vendor, a talk NPC, or someone's pet — never
    /// fought. Settled on the wire by two independent askers (HuntController's hunting and
    /// MissionRun's fight-or-run outside buildings); the masks must never diverge between them.
    /// Each asker keeps its own EXTRA conditions (level caps, foreign fights, mission context) on
    /// top of this shared kind test.
    /// </summary>
    public static class MobFilter
    {
        public const int FlagSells = 0x200000;   // has a vendor inventory
        public const int FlagTalk = 0x800000;    // a talk NPC
        public const int FlagPet = 0x8000000;    // someone's pet
        public const int SideMonster = 3;        // the monster side

        /// <summary>
        /// Kind test only: not a vendor/talk NPC and not someone's pet (owner-attached or pet-type).
        /// Says nothing about side, level, or whose fight it is — the asker decides those.
        /// </summary>
        public static bool IsFightableKind(SimpleChar n)
        {
            int flags = (int)n.Flags;
            if ((flags & (FlagSells | FlagTalk | FlagPet)) != 0) return false;
            if (n is NpcChar npc && (npc.Owner.HasValue || npc.PetTypeId != 0)) return false;
            return true;
        }
    }
}