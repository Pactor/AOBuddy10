// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MonsterInfoPacket.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the MonsterInfoPacket type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// The examine record of a non-player (NotAPlayer 0x10 set, no ACG items or suppression): every mission NPC
    /// examined in the recordings. Carries the full <see cref="InfoPacket"/> record - notably Health/MaxHealth -
    /// which the AOSharp original left entirely unread.
    /// </summary>
    public class MonsterInfoPacket : InfoPacket
    {
    }
}
