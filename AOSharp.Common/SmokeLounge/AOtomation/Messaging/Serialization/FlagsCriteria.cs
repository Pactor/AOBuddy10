// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FlagsCriteria.cs" company="SmokeLounge">
//   Copyright � 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FlagsCriteria type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization
{
    public enum FlagsCriteria
    {
        HasAll, 

        HasAny, 

        EqualsToAny, 

        Default,

        /// <summary>
        /// Present when none of the listed bits is set (OmniCell's SerializationContext.EvaluateHasNone:
        /// every value v satisfies (v &amp; flag) == 0). Added last so the existing values keep their numbers;
        /// DoorFullUpdateMessage gates its position on the owner instance with it.
        /// </summary>
        HasNone,

        /// <summary>
        /// Present when the flag equals none of the listed values (OmniCell's NotEqualsToAny).
        /// Added after HasNone so the existing values keep their numbers; MailRecord gates its
        /// attachment block on it (the client reads on only when the byte is not 1).
        /// </summary>
        NotEqualsToAny
    }
}