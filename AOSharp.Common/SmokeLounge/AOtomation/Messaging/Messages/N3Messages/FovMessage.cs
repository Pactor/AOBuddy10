// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FovMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FovMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\FovMessage.cs); no
    // changes.
    // AOBuddy census 2026-09-29: 427 copies in the bot's mission recordings and 234 in the retail dumps, every one
    // read to the last byte by this class.

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Tells an NPC to start looking around, and describes the sweep.
    /// </summary>
    /// <remarks>
    /// Thirty three bytes, always, and there is no room in it for anything
    /// conditional. The reader is at Gamecode.dll 0x10073BA2: it allocates a
    /// thirty six byte block and fills it with two floats, one byte, five more
    /// floats and an int32 - thirty six in memory rather than thirty three
    /// because of the padding after the byte, which is not on the wire. Its
    /// WriteSubClass is a bare return, so the client never sends this.
    ///
    /// The dispatcher at 0x10073C32 resolves the identity to a dynel, hands the
    /// block to the object at dynel + 0x1D8, and sets stat 533, npcfovstatus,
    /// to 1.
    ///
    /// What the block means comes from the two functions that touch it. The one
    /// that receives it, at 0x100524F4, tidies it up: it forces TurnDownSpeed
    /// and TurnUpSpeed positive, defaults TurnSpeed to TurnUpSpeed when it is
    /// zero, clamps AtLimit to 0-2, and - when SweepCentre is above zero -
    /// replaces it with the direction the NPC is actually facing and copies
    /// that into CurrentAngle.
    ///
    /// The other, at 0x10052332, is the sweep itself, run every frame:
    ///
    ///     CurrentAngle += TurnSpeed * elapsed
    ///     if (CurrentAngle left the arc SweepCentre +/- SweepHalfAngle)
    ///         do what AtLimit says
    ///
    /// and then it builds a rotation about the up axis by CurrentAngle and
    /// hands it to the dynel as its heading. So this message is an NPC turning
    /// its head back and forth, and the fields are the arc, the speed and where
    /// in the arc it currently is.
    ///
    /// The captured values agree field for field. Both NPCs carry AtLimit 0,
    /// Bounce, and their TurnSpeed is only ever plus or minus their own
    /// TurnUpSpeed - which is exactly what Bounce does and nothing else would.
    /// The angles land on whole degrees: 65 and 22.5 for the two arcs, 25 and
    /// 20 for the two speeds.
    ///
    /// 391 of these appear in a single captured mission run and not one appears
    /// in any earlier capture, because nothing before it had a pet fighting
    /// alongside NPCs that could notice it.
    /// </remarks>
    [AoContract((int)N3MessageType.Fov)]
    public class FovMessage : N3Message
    {
        #region Constructors and Destructors

        public FovMessage()
        {
            this.N3MessageType = N3MessageType.Fov;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Read into the block and never read back out of it. 15 degrees on one
        /// captured NPC and 22 on the other.
        /// </summary>
        /// <remarks>
        /// This and Unknown9 are the two fields the client does not use. Both
        /// functions that touch the block are accounted for - 0x100524F4 and
        /// the sweep at 0x10052332 - and between them they read offsets 4, 8,
        /// 0xC, 0x10, 0x14, 0x18 and 0x1C. Offset 0, which is this, and offset
        /// 0x20 are written by the reader and never looked at again. The block
        /// is not handed to anything else, so there is nowhere else to look.
        ///
        /// It is a float in the same units as every other angle here, it sits
        /// at the front of a message about what an NPC can see, and 15 and 22
        /// degrees are the size of a vision cone rather than of a sweep. That
        /// is a reason to suspect it and not a reason to name it.
        /// </remarks>
        [AoMember(0)]
        public float Unknown1 { get; set; }

        /// <summary>
        /// Half the arc the head sweeps through, in radians. 65 degrees on one
        /// captured NPC and 22.5 on the other.
        /// </summary>
        /// <remarks>
        /// The sweep's limits are SweepCentre plus and minus this: 0x10052370
        /// builds the upper one and 0x1005240F the lower.
        /// </remarks>
        [AoMember(1)]
        public float SweepHalfAngle { get; set; }

        /// <summary>
        /// What the head does at the end of the arc. Bounce in every captured
        /// copy.
        /// </summary>
        [AoMember(2)]
        public FovLimitBehaviour AtLimit { get; set; }

        /// <summary>
        /// How fast the head turns back down from the upper limit, in radians
        /// per second. 25 degrees on one captured NPC and 20 on the other.
        /// </summary>
        /// <remarks>
        /// Reaching the upper limit sets TurnSpeed to minus this, at
        /// 0x10052405. 0x1005258E forces it positive on arrival, so a negative
        /// one on the wire would be taken as its own magnitude.
        /// </remarks>
        [AoMember(3)]
        public float TurnDownSpeed { get; set; }

        /// <summary>
        /// How fast the head turns back up from the lower limit, in radians per
        /// second. Equal to TurnDownSpeed in every captured copy.
        /// </summary>
        /// <remarks>
        /// Reaching the lower limit sets TurnSpeed to plus this, at 0x10052456,
        /// and 0x100525A3 forces it positive on arrival. It is also what
        /// TurnSpeed falls back to when the wire carries a zero there.
        /// </remarks>
        [AoMember(4)]
        public float TurnUpSpeed { get; set; }

        /// <summary>
        /// The direction the arc is centred on, in radians. 234 degrees on one
        /// captured NPC and 270.5 on the other.
        /// </summary>
        /// <remarks>
        /// The one field the receiver overrides: 0x1005251C tests it against
        /// zero and, when it is above it, throws the sent value away and works
        /// the centre out from the direction the NPC is already facing, then
        /// copies the result into CurrentAngle. Below or at zero the sent value
        /// stands.
        /// </remarks>
        [AoMember(5)]
        public float SweepCentre { get; set; }

        /// <summary>
        /// Where in the arc the head is right now, in radians.
        /// </summary>
        /// <remarks>
        /// The only field that differs across all 391 captured copies, which is
        /// what a continuously animated angle looks like. The sweep advances it
        /// by TurnSpeed times the frame time and turns it into the dynel's
        /// heading.
        /// </remarks>
        [AoMember(6)]
        public float CurrentAngle { get; set; }

        /// <summary>
        /// How fast the head is turning right now, in radians per second, and
        /// signed - which way it is going.
        /// </summary>
        /// <remarks>
        /// Four values across the captures and all four are plus or minus one
        /// of the two speeds above, one pair per NPC, split almost evenly
        /// between the signs. That is a head bouncing between two limits.
        /// </remarks>
        [AoMember(7)]
        public float TurnSpeed { get; set; }

        /// <summary>
        /// Read into the block and never read back out of it. 13 on one
        /// captured NPC and 20 on the other.
        /// </summary>
        /// <remarks>
        /// The other field the client does not use - see Unknown1 for why that
        /// is certain rather than merely unobserved.
        ///
        /// Two values from two NPCs is not enough to tell a sight range from a
        /// level from a count, and guessing between them would put a name on
        /// the packet that nothing supports.
        /// </remarks>
        [AoMember(8)]
        public int Unknown9 { get; set; }

        #endregion
    }
}
