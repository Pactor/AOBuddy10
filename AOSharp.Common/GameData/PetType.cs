namespace AOSharp.Common.GameData
{
    /// <summary>
    /// What a pet is FOR, as the server states it. It arrives in every pet's SimpleCharFullUpdate
    /// (SimpleNpcInfo.PetType) and again in PetToMasterMessage.AttachNotificationValue on attach.
    ///
    /// These values were already correct here, but nothing ever populated them: the wire reader read
    /// the pet-type byte and threw it away, so pet roles had to be guessed from position or name.
    /// NpcChar.Role now carries it. Confirmed against roughly a hundred pets in sniffs/ and captures/,
    /// across Meta-Physicists, Engineers and Bureaucrats - in captures/mp_203534_s37.csv the Heal pet
    /// is seen casting on the Attack pet while the Support pet debuffs the mob.
    /// </summary>
    public enum PetType
    {
        // 0 for an ordinary NPC that merely has a master, rather than a pet in one of the real slots.
        Unknown = 0x0,
        /// <summary>The fighting pet: Bureaucrat Worker, Engineer Automaton, MP Rihwen/Demon.</summary>
        Attack = 0xA,

        /// <summary>The healing pet: MP Mortificant/Restite/Salvinous, monster template 96193.</summary>
        Heal = 0xB,

        /// <summary>The crowd-control/debuff pet: MP Yidira, Distracting Sphere, Carlo Pinnetti.</summary>
        Support = 0xC,

        /// <summary>A vanity pet, not a combat one (balloons, boards).</summary>
        Social = 0xE
    }
}
