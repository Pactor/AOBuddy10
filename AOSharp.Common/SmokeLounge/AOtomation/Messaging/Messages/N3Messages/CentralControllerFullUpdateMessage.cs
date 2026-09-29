// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CentralControllerFullUpdateMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CentralControllerFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging
    // (Messages\N3Messages\CentralControllerFullUpdateMessage.cs). Changes for this library only: AOSharp's
    // Identity/Vector3/Quaternion types; the Stat enum as the stat key (OmniCell: CharacterStat); nullable
    // Coordinate/Heading for the instance-gated position, as DoorFullUpdateMessage declares it; ArraySizeType.Int32
    // where OmniCell says Int32Terminated - this library has no Int32Terminated, and its string reader trims the
    // counted NUL, so the read is byte for byte the same; only a re-serialized string would come out one byte short of
    // the client's writer.
    // AOBuddy census 2026-09-29: 0 copies in the bot's mission recordings and 20 in the retail dumps, every one read
    // to the last byte by this class.

    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A mine, sentry or fence controller, and whether it is still running.
    /// </summary>
    /// <remarks>
    /// A SimpleItemFullUpdate with two bytes on the end. Its reader at
    /// Gamecode.dll 0x1009F466 calls SimpleItemFullUpdateIIR_t::ReadSubClass at
    /// 0x100A1661 and then reads the two, so everything down to Name is that
    /// message field for field - the fourth member of a family that already had
    /// SimpleItem, Chest and VendingMachine in it.
    ///
    /// The two bytes name themselves. The client builds the controller's display
    /// name out of them with "%s %s controller", taking the first half from the
    /// state and the second from the kind, and a captured controller carries
    /// kind 2 and state 0 alongside a Name field that reads "Active sentry
    /// controller".
    /// </remarks>
    [AoContract((int)N3MessageType.CentralControllerFullUpdate)]
    public class CentralControllerFullUpdateMessage : N3Message
    {
        #region Fields

        private int identityType;

        private int instance;

        #endregion

        #region Constructors and Destructors

        public CentralControllerFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.CentralControllerFullUpdate;
        }

        #endregion

        #region Public Properties

        public Identity Owner { get; set; }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// 11, checked against a static, as in every message of this family.
        /// </summary>
        [AoMember(1)]
        public int MsgVersion { get; set; }

        [AoMember(2)]
        public int Identitytype
        {
            get
            {
                return this.identityType;
            }

            set
            {
                this.identityType = value;
                this.Owner = new Identity { Type = (IdentityType)value, Instance = this.instance };
            }
        }

        /// <summary>
        /// Zero is what makes the position fields present. See
        /// SimpleItemFullUpdate: it is the instance and not the type.
        /// </summary>
        [AoMember(3)]
        [AoFlags("flag")]
        public int Instance
        {
            get
            {
                return this.instance;
            }

            set
            {
                this.instance = value;
                this.Owner = new Identity { Type = (IdentityType)this.identityType, Instance = value };
            }
        }

        [AoMember(4)]
        [AoUsesFlags("flag", typeof(Vector3), FlagsCriteria.HasNone, new[] { int.MaxValue })]
        public Vector3? Coordinate { get; set; }

        [AoMember(5)]
        [AoUsesFlags("flag", typeof(Quaternion), FlagsCriteria.HasNone, new[] { int.MaxValue })]
        public Quaternion? Heading { get; set; }

        [AoMember(6)]
        public int Playfield { get; set; }

        /// <summary>
        /// The shared item-message constant. See ItemMessageConstants.
        /// </summary>
        [AoMember(7)]
        public Identity Marker { get; set; }

        /// <summary>
        /// Stat 55, inventoryid.
        /// </summary>
        [AoMember(8)]
        public byte InventoryId { get; set; }

        /// <summary>
        /// Stat 220, currbodylocation.
        /// </summary>
        [AoMember(9)]
        public byte BodyLocation { get; set; }

        [AoMember(10, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<Stat, uint>[] Stats { get; set; }

        [AoMember(11, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        /// <summary>
        /// What it controls. The reader refuses anything above 3.
        /// </summary>
        [AoMember(12)]
        public CentralControllerKind Kind { get; set; }

        /// <summary>
        /// Whether it is running. The reader refuses anything above 2, and hands
        /// it to the same setter CentralControllerState uses.
        /// </summary>
        [AoMember(13)]
        public CentralControllerStatus Status { get; set; }

        #endregion
    }
}
