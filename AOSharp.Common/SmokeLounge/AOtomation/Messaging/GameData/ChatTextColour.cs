// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatTextColour.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ChatTextColour type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// Which of the client's chat colours a line is printed in.
    /// </summary>
    /// <remarks>
    /// The names are the client's, spelled the way it spells them, with the CC
    /// prefix dropped. GUI.dll keeps a table of them at 0x10268D38, and the
    /// chat window's own AddLine at 0x1009BAD0 is what uses it: given a
    /// non-zero colour it looks the value up there and wraps the line in
    /// &lt;font color=NAME&gt;.
    ///
    /// The path from the wire to that table is three hops. ChatText's
    /// dispatcher hands the byte to the engine's chat text signal as its third
    /// argument; GUI.dll subscribes to that signal at 0x10083E7B; and the
    /// subscriber finds the window named by the first argument and calls
    /// AddLine with the text and this.
    ///
    /// None is not a colour. AddLine skips the font tag entirely when the value
    /// is zero, so the line goes out in whatever the window's own colour is.
    /// </remarks>
    public enum ChatTextColour : byte
    {
        /// <summary>
        /// The client calls it CCNoneColor.
        /// </summary>
        None = 0,

        /// <summary>
        /// The client calls it CCMenubarColor.
        /// </summary>
        Menubar = 1,

        /// <summary>
        /// The client calls it CCWhisperColor.
        /// </summary>
        Whisper = 2,

        /// <summary>
        /// The client calls it CCShoutColor.
        /// </summary>
        Shout = 3,

        /// <summary>
        /// The client calls it CCTellColor.
        /// </summary>
        Tell = 4,

        /// <summary>
        /// The client calls it CCVicinityColor.
        /// </summary>
        Vicinity = 5,

        /// <summary>
        /// The client calls it CCCommColor.
        /// </summary>
        Comm = 6,

        /// <summary>
        /// The client calls it CCTeamColor.
        /// </summary>
        Team = 7,

        /// <summary>
        /// The client calls it CCClanColor.
        /// </summary>
        Clan = 8,

        /// <summary>
        /// The client calls it CCEmoteColor.
        /// </summary>
        Emote = 9,

        /// <summary>
        /// The client calls it CCLinkColor.
        /// </summary>
        Link = 10,

        /// <summary>
        /// The client calls it CCToolTipColor.
        /// </summary>
        ToolTip = 11,

        /// <summary>
        /// The client calls it CCRed.
        /// </summary>
        Red = 12,

        /// <summary>
        /// The client calls it CCGreen.
        /// </summary>
        Green = 13,

        /// <summary>
        /// The client calls it CCBlue.
        /// </summary>
        Blue = 14,

        /// <summary>
        /// The client calls it CCWhite.
        /// </summary>
        White = 15,

        /// <summary>
        /// The client calls it CCYellow.
        /// </summary>
        Yellow = 16,

        /// <summary>
        /// The client calls it CCCashColor.
        /// </summary>
        Cash = 17,

        /// <summary>
        /// The client calls it CCInfoHeadline.
        /// </summary>
        InfoHeadline = 18,

        /// <summary>
        /// The client calls it CCInfoHeader.
        /// </summary>
        InfoHeader = 19,

        /// <summary>
        /// The client calls it CCInfoText.
        /// </summary>
        InfoText = 20,

        /// <summary>
        /// The client calls it CCMeHitByNanoColor.
        /// </summary>
        MeHitByNano = 21,

        /// <summary>
        /// The client calls it CCOtherHitByNanoColor.
        /// </summary>
        OtherHitByNano = 22,

        /// <summary>
        /// The client calls it CCMonsterHitMeColor.
        /// </summary>
        MonsterHitMe = 23,

        /// <summary>
        /// The client calls it CCPlayerHitMeColor.
        /// </summary>
        PlayerHitMe = 24,

        /// <summary>
        /// The client calls it CCMeHitOtherColor.
        /// </summary>
        MeHitOther = 25,

        /// <summary>
        /// The client calls it CCOtherHitOtherColor.
        /// </summary>
        OtherHitOther = 26,

        /// <summary>
        /// The client calls it CCOtherHitOtherMyPetColor.
        /// </summary>
        OtherHitOtherMyPet = 27,

        /// <summary>
        /// The client calls it CCMeHealedColor.
        /// </summary>
        MeHealed = 28,

        /// <summary>
        /// The client calls it CCMeGotXpColor.
        /// </summary>
        MeGotXp = 29,

        /// <summary>
        /// The client calls it CCSkillColor.
        /// </summary>
        Skill = 30,

        /// <summary>
        /// The client calls it CCShowFullNameColor.
        /// </summary>
        ShowFullName = 31,

        /// <summary>
        /// The client calls it CCCCHeaderColor.
        /// </summary>
        CCHeader = 32,

        /// <summary>
        /// The client calls it CCCCTextColor.
        /// </summary>
        CCText = 33,

        /// <summary>
        /// The client calls it CCTowerColor.
        /// </summary>
        Tower = 34,

        /// <summary>
        /// The client calls it CCMeCastNano.
        /// </summary>
        MeCastNano = 35
    }
}
