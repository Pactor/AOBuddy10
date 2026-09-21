// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RaidCmdType.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the RaidCmdType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    public enum RaidCmdType : int
    {
        CreateRaid = 0x01,
        ShowMemberList = 0x2,
        MoveMember = 0x4,
        RequestLocks = 0x06
    }
}