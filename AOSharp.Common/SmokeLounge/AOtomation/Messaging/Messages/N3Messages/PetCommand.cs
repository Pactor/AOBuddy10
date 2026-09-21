// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PetCommand.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PetCommand type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    public enum PetCommand : int
    {
        Follow = 1,
        Behind = 2,
        Wait = 4,
        Guard = 6,
        Attack = 7,
        Social = 9,
        Terminate = 10,
        Release = 11,
        Heal = 12,
        Report = 14,
        Chat = 16
    }
}