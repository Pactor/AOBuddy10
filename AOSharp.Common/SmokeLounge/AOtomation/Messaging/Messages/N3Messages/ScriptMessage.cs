// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScriptMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ScriptMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\ScriptMessage.cs); no
    // changes.

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A visual effect script for the client to run.
    /// </summary>
    /// <remarks>
    /// ScriptIIR_t has vtable 0x1017231C, reader 0x101308EE, writer 0x10130852
    /// and dispatcher 0x10130A1B. The reader and the writer emit the same ten
    /// fields in the same order, so there are two independent sources for the
    /// layout: three int32s, a counted string, three int32s, a counted string
    /// and two int32s. Both strings go through the shared helper at 0x10038AF8,
    /// which is an int16 length and then the characters with no terminator.
    ///
    /// What the message is comes out of one import. The dispatcher builds a
    /// string from the script text and hands it to
    /// FXS_t::ExecuteScript(char const*, FXS_Object_t*) - FXS being the
    /// client's effect scripting system - and files the object that comes back
    /// in a map at 0x102EACA8 keyed by the script id. So this is the server
    /// starting, superceding and stopping visual effects by name.
    ///
    /// The clock handling at the top of the dispatcher runs for every command
    /// and is what names three of the numbers. It takes the client's own time,
    /// subtracts <see cref="ServerTime"/> from it, keeps the smallest
    /// difference it has ever seen in the static at 0x102EACA4, and adds that
    /// to <see cref="StartTime"/> - the standard trick for turning a server
    /// clock reading into a client one using the least-delayed sample. Both
    /// times are then fed into the script text, which is an LDB format
    /// template: LDBformat takes the text, Feed takes
    /// <see cref="Duration"/> and then the corrected
    /// <see cref="StartTime"/>, and Dump produces what ExecuteScript runs.
    /// Command 3 uses the same two the other way round, computing
    /// Duration - now + StartTime and handing that remaining time to the
    /// playfield with the script.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.Script)]
    public class ScriptMessage : N3Message
    {
        #region Constructors and Destructors

        public ScriptMessage()
        {
            this.N3MessageType = N3MessageType.Script;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The id the running effect is filed under.
        /// </summary>
        /// <remarks>
        /// The key into the map at 0x102EACA8, which holds one FXS_Object_t per
        /// live script. Command 3 clears the entry and the running commands
        /// write it.
        /// </remarks>
        [AoMember(0)]
        public int ScriptId { get; set; }

        /// <summary>
        /// The id of the effect to supercede.
        /// </summary>
        /// <remarks>
        /// Looked up in the same map at 0x10130CF1. Commands 1 and 2 read
        /// SupercedeTime off its class, take its child named Life and read that
        /// child's StartTime and LifeTime, and when more than SupercedeTime has
        /// elapsed they back-date the child so the effect jumps past that
        /// point.
        /// </remarks>
        [AoMember(1)]
        public int SupercedeScriptId { get; set; }

        /// <summary>
        /// Which of the five things to do.
        /// </summary>
        [AoMember(2)]
        public ScriptCommand Command { get; set; }

        /// <summary>
        /// The script itself, as an LDB format template.
        /// </summary>
        /// <remarks>
        /// LDBformat is handed this text, then fed <see cref="Duration"/> and
        /// the corrected <see cref="StartTime"/>, and what Dump returns is what
        /// FXS_t::ExecuteScript runs.
        /// </remarks>
        [AoMember(3, SerializeSize = ArraySizeType.Int16)]
        public string ScriptText { get; set; }

        /// <summary>
        /// When the effect started, on the server's clock.
        /// </summary>
        /// <remarks>
        /// The dispatcher adds its running estimate of the clock offset to this
        /// at 0x10130A65 before using it, so it arrives in server terms.
        /// </remarks>
        [AoMember(4)]
        public int StartTime { get; set; }

        /// <summary>
        /// How long the effect lasts.
        /// </summary>
        /// <remarks>
        /// Fed to the template first, and used by command 3 as
        /// Duration - now + StartTime, which is the time the effect has left.
        /// </remarks>
        [AoMember(5)]
        public int Duration { get; set; }

        /// <summary>
        /// The server's clock at the moment it sent this.
        /// </summary>
        /// <remarks>
        /// The client subtracts it from its own clock and keeps the smallest
        /// difference it has seen as the offset between the two.
        /// </remarks>
        [AoMember(6)]
        public int ServerTime { get; set; }

        /// <summary>
        /// The effect's name, as the playfield files it.
        /// </summary>
        /// <remarks>
        /// Command 3 runs it through LDBface::ElfHash and hands the hash to the
        /// playfield with the script; command 1 uses the same hash to take it
        /// away again.
        /// </remarks>
        [AoMember(7, SerializeSize = ArraySizeType.Int16)]
        public string Name { get; set; }

        /// <summary>
        /// Where in the world the effect is, along X.
        /// </summary>
        /// <remarks>
        /// Only <see cref="ScriptCommand.RunIfNear"/> reads this and
        /// <see cref="PositionZ"/>, comparing the squared distance from the
        /// player's own position against 0x9C40 - two hundred units squared.
        /// </remarks>
        [AoMember(8)]
        public int PositionX { get; set; }

        /// <summary>
        /// And along the second horizontal axis.
        /// </summary>
        /// <remarks>
        /// The client takes Vehicle_t::GetGlobalPos and uses its first and
        /// third components - the horizontal plane, height being the second -
        /// each truncated to an integer before the comparison.
        /// </remarks>
        [AoMember(9)]
        public int PositionZ { get; set; }

        #endregion
    }
}
