// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FightModeName.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FightModeName type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    public class FightModeName
    {
        #region AoMember Properties

        [AoMember(0)]
        public int Unknown1 { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Text { get; set; }
      
        [AoMember(2)]
        public byte Unknown2 { get; set; }

        [AoMember(3)]
        public byte Unknown3 { get; set; }
        #endregion
    }
}