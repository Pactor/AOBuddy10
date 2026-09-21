// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleCharacterInfo.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SimpleCharInfo type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    public class SimpleCharInfo
    {
        public class NPCInfo : SimpleCharInfo
        {
            public short Family { get; set; }
            public short LosHeight { get; set; }
        }

        public class PlayerInfo : SimpleCharInfo
        {
            public uint CurrentNano { get; set; }
            public int Team { get; set; }
            public short Swim { get; set; }
            public short StrengthBase { get; set; }
            public short AgilityBase { get; set; }
            public short StaminaBase { get; set; }
            public short IntelligenceBase { get; set; }
            public short SenseBase { get; set; }
            public short PsychicBase { get; set; }

            // Size Int16
            public string FirstName { get; set; }
            // Size Int16
            public string LastName { get; set; }
            // Size Int16
            public string OrgName { get; set; }
            public int  OrgId { get; set; }
        }

        public class SpecialAttackData
        {
            public short Unknown1 { get; set; }
            public short Unknown2 { get; set; }
            public short Unknown3 { get; set; }
            public short Unknown4 { get; set; }
            public short Unknown5 { get; set; }
            public string Name { get; set; }
            public short Unknown6 { get; set; }
        }

        public class TextureOverride
        {
            public string Name { get; set; }
            public int TextureId;
            public int Unknown1;
            public int Unknown2;
        }
        public class ActiveNano
        {
            public Identity Identity {  get; set; }
            public int NanoInstance { get; set; }
            public int Time1 { get; set; }
            public int Time2 { get; set; }
        }
    }
}