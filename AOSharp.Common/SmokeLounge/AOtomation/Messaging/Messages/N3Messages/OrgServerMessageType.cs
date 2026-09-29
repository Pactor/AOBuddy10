// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OrgServerMessageType.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OrgServerMessageType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    /// <summary>
    /// Which of the nine shapes an OrgServer carries (ported from OmniCell).
    /// </summary>
    /// <remarks>
    /// The reader at Gamecode 0x10126D95 reads this byte, widens it, and fails
    /// the whole message unless it is between 1 and 9. AOSharp named three
    /// (OrgContract = 1, OrgInfo = 2, OrgInvite = 5); its OrgContract value was wrong:
    /// OmniCell's 404 captured copies are all kind 6, the contract, and kind 1 is a
    /// container listing. The six kinds without a name have never been captured.
    /// </remarks>
    public enum OrgServerMessageType : byte
    {
        OrgKind1 = 0x01,
        OrgInfo = 0x02,
        OrgKind3 = 0x03,
        OrgKind4 = 0x04,
        OrgInvite = 0x05,
        OrgContract = 0x06,
        OrgKind7 = 0x07,
        OrgKind8 = 0x08,
        OrgKind9 = 0x09,
    }
}
