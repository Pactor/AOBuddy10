// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatTextMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ChatTextMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Layout, names and doc comments ported from OmniCell's AOtomation.Messaging. AOSharp's Unknown1 short
    // was the two bytes Colour and OnScreen; old names kept as [Obsolete] aliases.
    [AoContract((int)N3MessageType.ChatText)]
    public class ChatTextMessage : N3Message
    {
        #region Constructors and Destructors

        public ChatTextMessage()
        {
            this.N3MessageType = N3MessageType.ChatText;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The line. A two byte count and no terminator.
        /// </summary>
        [AoMember(0, SerializeSize = ArraySizeType.Int16)]
        public string Text { get; set; }

        /// <summary>
        /// Which of the client's chat colours to print it in.
        /// </summary>
        /// <remarks>
        /// The dispatcher at Gamecode.dll 0x100388C7 hands this to the engine's
        /// chat text signal as its third argument; GUI.dll subscribes to that
        /// signal at 0x10083E7B, finds the window <see cref="Window"/> names,
        /// and calls its AddLine with the text and this. AddLine looks the
        /// value up in the colour table at 0x10268D38 and wraps the line in
        /// &lt;font color=NAME&gt; - or leaves the line alone when the value is
        /// zero, which is what makes None a real value rather than a missing
        /// one. A system announcement carries 16, which the table calls CCYellow.
        /// </remarks>
        [AoMember(1)]
        public SmokeLounge.AOtomation.Messaging.GameData.ChatTextColour Colour { get; set; }

        /// <summary>
        /// Where the line goes: 0 to a chat window, 1 to the screen.
        /// </summary>
        /// <remarks>
        /// The reader at 0x1003896D refuses anything above 1, and the
        /// dispatcher at 0x1003889D is a single branch on it. Zero takes the
        /// chat text signal above. One takes the other arm - the floating
        /// on-screen message, which has no window and ignores <see cref="Colour"/> entirely.
        /// </remarks>
        [AoMember(2)]
        public byte OnScreen { get; set; }

        /// <summary>
        /// Which chat window to print in.
        /// </summary>
        /// <remarks>
        /// The same routing value OrgClient carries in the other direction, in
        /// the same argument position of the same signal: the GUI subscriber at
        /// 0x10083E8E looks the value up as a window id when it is below
        /// 0x40000000 and treats it by name above that.
        /// </remarks>
        [AoMember(3)]
        public int Window { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        /// <summary>Old two-byte view: Colour in the high byte, OnScreen in the low byte (wire order).</summary>
        [Obsolete("Wire fields are Colour (byte) and OnScreen (byte).")]
        public short Unknown1
        {
            get => (short)(((byte)this.Colour << 8) | this.OnScreen);
            set { this.Colour = (SmokeLounge.AOtomation.Messaging.GameData.ChatTextColour)((value >> 8) & 0xFF); this.OnScreen = (byte)(value & 0xFF); }
        }

        [Obsolete("Wire field is Window.")]
        public int Unknown2 { get => this.Window; set => this.Window = value; }

        #endregion
    }
}
