// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WaypointPathMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the WaypointPathMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\WaypointPathMessage.cs).
    // Changes for this library only: AOSharp's Identity/Vector3/Quaternion types.

    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The route a vehicle is to fly, and how fast.
    /// </summary>
    /// <remarks>
    /// Reader 0x100041CB, writer 0x100040C2, dispatcher 0x10004074, vtable
    /// 0x10155DB0. The reader takes an X3F1 list of Vector3s through the helper
    /// at 0x10004296 and then one float; the writer emits the same two.
    ///
    /// The dispatcher resolves the message&#39;s identity with GetDynel and
    /// casts it to a Vehicle_t - by way of CharVehicle_t - which is the first
    /// thing worth knowing: this message is for vehicles, not for characters on
    /// foot. It then calls virtual slot 0x64 with the list and the float, and
    /// Vehicle.dll exports the name of that slot:
    /// Vehicle_t::UseWaypointPath(vector&lt;Vector3_t&gt; const&amp;, float) at
    /// 0x1000CDE1.
    ///
    /// What the float is comes out of what UseWaypointPath builds with it. The
    /// path object&#39;s constructor at 0x1000F412 keeps it at +0x14, walks the
    /// waypoints adding up the total length into +0x20, and then sets
    /// +0x18 = length / float. The path&#39;s own evaluator at 0x1000F37F is
    /// handed a time: it compares that time against +0x18 and returns the last
    /// waypoint once it is past it, and otherwise multiplies +0x1C by the time
    /// to get a distance along the route. So +0x18 is how long the path takes,
    /// and length divided by the float being a time makes the float a speed.
    ///
    /// SimpleCharFullUpdate carries a path too, and it is not this one: see
    /// <see cref="GameData.WaypointPath"/>, which has an Identity in front, a
    /// plain int32 count rather than the X3F1 form, and no speed.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.WaypointPath)]
    public class WaypointPathMessage : N3Message
    {
        #region Constructors and Destructors

        public WaypointPathMessage()
        {
            this.N3MessageType = N3MessageType.WaypointPath;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The route, in order.
        /// </summary>
        /// <remarks>
        /// Read at 0x100041D8. The helper refuses an X3F1 count that is not a
        /// multiple of 0x3F1 or that asks for more than 0x7530 entries, and
        /// reads three floats per waypoint at 0x1000404E.
        /// </remarks>
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public Vector3[] Waypoints { get; set; }

        /// <summary>
        /// How fast to travel it.
        /// </summary>
        /// <remarks>
        /// Read at 0x100041E6 and handed to Vehicle_t::UseWaypointPath as its
        /// second argument. See the class remarks for why it is a speed and not
        /// a duration.
        /// </remarks>
        [AoMember(1)]
        public float Speed { get; set; }

        #endregion
    }
}
