// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClientRequestBuildMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ClientRequestBuildMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging
    // (Messages\N3Messages\ClientRequestBuildMessage.cs). Changes for this library only: AOSharp's
    // Identity/Vector3/Quaternion types; this library's enum name ConstructBuilding for 0x53011416 (OmniCell:
    // ClientRequestBuild); the class keeps OmniCell's name.

    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Put a building up on one tile.
    /// </summary>
    /// <remarks>
    /// One of the five messages the city GUI sends, all built the same way. The
    /// sender takes the city's CityAI_c, calls GetID - exported from city.dll at
    /// 0x100027E2, returning a CityID_t - and pushes that structure's +4 into
    /// the message; CityID_t opens with an Identity, read by city.dll's own
    /// identity operator at 0x100274BB, so +4 is its instance half. Nothing else
    /// of the city id goes on the wire, because the server has the rest.
    ///
    /// The client is the only source for this message: it is sent, never
    /// received - the dispatcher does nothing but clear the pass-on flag - and
    /// no capture contains one, because a capture of a city would need a city.
    /// </remarks>
    /// <remarks>
    /// The other end is city.dll's
    /// CityAI_c::ClientRequestBuild(Identity const&amp;, Identity const&amp;,
    /// TilePos_c const&amp;, int, Identity const&amp;) at 0x10002A0C. Its first
    /// argument is the requester, which the server supplies; the four after it
    /// line up one for one with the four fields that follow the city id here,
    /// in order.
    ///
    /// The signature carries no parameter names, but the sending side does. The
    /// chain is: GUI.dll 0x100E90FC takes one entry of the city window's build
    /// plan and calls ClientRequestBuild with the player's Identity, the entry's
    /// + 8, + 0x10, + 0x18 and + 0, in that order; that reaches
    /// AOCityInterfaceClient_c slot 0x140, at Gamecode 0x1012E62F, which builds
    /// this message. So the plan entry is the message, and the entry is a
    /// BaseClientHouseData_t - the struct city.dll passes to and from
    /// CityClientInterface_c::FillCityHouseDataVector.
    ///
    /// Two of the three then name themselves, because the same GUI code resolves
    /// the entry through a registry whose lookup Funcom exported. See
    /// <see cref="HouseTemplate"/> and <see cref="Rotation"/>. The third does
    /// not; see <see cref="Unknown3"/>.
    ///
    /// Reader 0x1012FFFF, writer 0x10130053, constructor 0x101300A6,
    /// dispatcher 0x1013009D; vtable 0x10172130. Class name
    /// ClientRequestBuildIIR_c.
    ///
    /// Reader 0x1012FFFF, writer 0x10130053, constructor 0x101300A6,
    /// dispatcher 0x1013009D; vtable 0x10172130. Class name
    /// ClientRequestBuildIIR_c.
    /// </remarks>
    [AoContract((int)N3MessageType.ConstructBuilding)]
    public class ClientRequestBuildMessage : N3Message
    {
        #region Constructors and Destructors

        public ClientRequestBuildMessage()
        {
            this.N3MessageType = N3MessageType.ConstructBuilding;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Which city, by the instance half of its CityID_t.
        /// </summary>
        /// <remarks>
        /// The only field. See the class remarks for where it comes from.
        /// </remarks>
        [AoMember(0)]
        public int CityInstance { get; set; }

        /// <summary>
        /// Which house to put up. The id of a CityHouseTemplate_c.
        /// </summary>
        /// <remarks>
        /// This was a guess for a long time - a house is built from a
        /// CityHouseTemplate_c, so a template identity is what it looked like -
        /// and a guess is what it stayed, because the receiving side of this
        /// message is a virtual call with an unnamed signature.
        ///
        /// The sending side settles it, and the name is Funcom's. GUI.dll
        /// 0x100E9073 takes the same build-plan entry this message is made of,
        /// fetches CityHouseTemplateRegistry_c::GetTemplateRegistry, and calls
        /// the registry's virtual slot 4 with the entry's + 8 and its + 0x18.
        /// That slot is
        /// CityHouseTemplateRegistry_c::GetTemplate(Identity const&amp;, int),
        /// exported from city.dll at 0x1000F0FD. Entry + 8 is this field, so
        /// this field is the Identity a house template is looked up by.
        ///
        /// The result goes straight to CityHouseTemplate_c::IsHQ, so it really is
        /// a template that comes back. House_c::GetTemplateID, exported at
        /// 0x1001291F, is the same identity on a house that has already been
        /// built.
        /// </remarks>
        [AoMember(1)]
        public Identity HouseTemplate { get; set; }

        /// <summary>
        /// The tile to build on.
        /// </summary>
        /// <remarks>
        /// Read at 0x10130027 through city.dll's TilePos_c operator, and the
        /// third argument of ClientRequestBuild, whose type the export names.
        /// </remarks>
        [AoMember(2)]
        public TilePos Tile { get; set; }

        /// <summary>
        /// Which way round to put it. The template's rotation.
        /// </summary>
        /// <remarks>
        /// The second half of the registry lookup described on
        /// <see cref="HouseTemplate"/>: GetTemplate takes an Identity and an
        /// int, and this field is the int.
        ///
        /// What that int is comes out of the other end of the registry.
        /// CityHouseTemplateRegistry_c::RegisterTemplate at city.dll 0x1000ECBA
        /// builds its map key from three words - the Identity it is given, and
        /// the template's own member + 0 - and CityHouseTemplate_c::GetRotation,
        /// exported at 0x1000E05D, is <c>return this-&gt;+0</c>. So the registry
        /// is keyed on a template id and a rotation, and this is the rotation.
        /// HasTemplate and DeregisterTemplate take the same pair.
        ///
        /// House_c::GetRotation, exported at 0x10012930, is the same quantity on
        /// a house already standing.
        /// </remarks>
        [AoMember(3)]
        public int Rotation { get; set; }

        /// <summary>
        /// The one field still unnamed, and the client can only ever send it
        /// empty.
        /// </summary>
        /// <remarks>
        /// Read at 0x1013003D, and it is the build-plan entry's + 0 - the first
        /// word of the BaseClientHouseData_t, ahead of the template id.
        ///
        /// What the client will not tell us is what a non-empty one means,
        /// because it never sends one. Both call sites into the sender, at
        /// GUI.dll 0x100E4A5F and 0x100E4A90, are guarded on this field being
        /// zero: 0x100E90DB is <c>return entry-&gt;+0 == 0 &amp;&amp;
        /// entry-&gt;+4 == 0</c>, and an entry that fails it is skipped. The
        /// window holds both the houses that already exist and the ones being
        /// placed, and only the ones with nothing here are submitted - so this
        /// is something a built house has and a planned one does not.
        ///
        /// It is not the house's template or tile or rotation, which are the
        /// three fields beside it. House_c has one more identity of its own,
        /// GetInsideID at city.dll 0x100129D7, and a house that does not exist
        /// has no interior yet, which fits - but House_c's own layout does not
        /// match this struct's, so that is a shape argument and not a proof, and
        /// it is not claimed here.
        /// </remarks>
        [AoMember(4)]
        public Identity Unknown3 { get; set; }

        #endregion
    }
}
