// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClientContainerAddItem.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ContainerAddItem type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.ContainerAddItem)]
    public class ContainerAddItem : N3Message
    {
        #region Constructors and Destructors

        public ContainerAddItem()
        {
            this.N3MessageType = N3MessageType.ContainerAddItem;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public Identity Source { get; set; }

        [AoMember(1)]
        public Identity Target { get; set; }

        [AoMember(2)]
        public int Slot { get; set; }
        #endregion
    }
}