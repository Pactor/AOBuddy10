// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FormatFeedbackMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FormatFeedbackMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using AOSharp.Common.Unmanaged.DataTypes;
    using AOSharp.Common.Unmanaged.Imports;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
    using System.Text;
    using System;

    [AoContract((int)N3MessageType.FormatFeedback)]
    public class FormatFeedbackMessage : N3Message
    {
        #region Constructors and Destructors

        public FormatFeedbackMessage()
        {
            this.N3MessageType = N3MessageType.FormatFeedback;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int Unknown1 { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Message { get; set; }
      
        [AoMember(2)]
        public int Unknown2 { get; set; }
        #endregion

        private string _formattedMessage = null;
        public string FormattedMessage
        {
            get
            {
                if (_formattedMessage == null)
                    _formattedMessage = FormatMessage();

                return _formattedMessage;
            }
        }

        private string FormatMessage()
        {
            StdString stdStr = StdString.Create();
            RemoteFormat.ParseString(stdStr.Pointer, Message);
            string formattedMessage = stdStr.ToString();
            stdStr.Dispose();

            return formattedMessage;
        }
    }
}