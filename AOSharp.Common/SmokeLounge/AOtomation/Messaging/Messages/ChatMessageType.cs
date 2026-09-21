// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatPacketType.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ChatPacketType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages
{
    public enum ChatMessageType : short
    {
        ServerSalt = 0,
        LoginRequest = 2,
        SelectCharacter = 3,
        LoginOK = 5,
        LoginError = 6,
        CharacterList = 7,
        CharacterName = 20,
        LookupMessage = 21,
        PrivateMessage = 30,
        VicinityMessage = 34,
        NpcMessage = 35,
        FriendAdd = 40,
        FriendStatus = 40,
        FriendRemove = 41,
        PrivateGroupInvite = 50,
        PrivateGroupInviteAccept = 52,
        PrivateGroupInviteDecline = 53,
        PrivateGroupMessage = 57,
        ChannelList = 60,
        GroupMessage = 65,
        Ping = 100,
        LftActivate = 1500,
        LftQueryResponse = 1501,
        LftDeactivate = 1501,
        LftQuery = 1502
    }
}