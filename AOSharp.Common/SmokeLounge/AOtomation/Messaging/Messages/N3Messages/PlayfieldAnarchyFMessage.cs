// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldAnarchyFMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PlayfieldAnarchyFMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The zone-in message: where the character lands, which playfield, and the playfield's generator record.
    /// </summary>
    /// <remarks>
    /// Read since 2026-09-29 by <see cref="Serialization.Serializers.Custom.PlayfieldAnarchyFMessageSerializer"/>,
    /// ported from OmniCell (Messages\N3Messages\PlayfieldAnarchyFMessage.cs and
    /// Serialization\Serializers\Custom\PlayfieldAnarchyFSerializer.cs), which reads every capture to the last byte.
    ///
    /// Wire order: int Version; Vector3 CharacterCoordinates; when Version &gt; 1: byte TokenMarker (0x61),
    /// Identity ModelId, int Group, int Subgroup, Identity PlayfieldId; when Version &gt; 3: a DbObject that reads
    /// itself - an Identity (empty = nothing follows), then int Revision and the object, by identity type:
    /// 51103 <see cref="BuildingGeneratorData"/> (a generated building: every mission), 51069
    /// <see cref="PlayfieldTemplateGeneratorData"/> (a static playfield's dynel runs, e.g. a Fair Trade), 51067
    /// <see cref="OwnedBuildingGeneratorData"/> (an owned building); then int PlayfieldX, int PlayfieldZ.
    ///
    /// The AOSharp original read the generator identity as <see cref="UnknownIdType"/>/<see cref="UnknownIdInstance"/>,
    /// the Revision and the next word as <see cref="PlayfieldX"/>/<see cref="PlayfieldZ"/>, and never read the
    /// building generator (the whole mission layout, up to 221 bytes, left unread) nor the real trailing
    /// PlayfieldX/PlayfieldZ. The old member names are kept below; PlayfieldX/PlayfieldZ now carry the real trailing
    /// values (-1, -1 in every mission capture), not the generator's revision/version.
    /// </remarks>
    [AoContract((int)N3MessageType.PlayfieldAnarchyF)]
    public class PlayfieldAnarchyFMessage : N3Message
    {
        #region Constructors and Destructors

        public PlayfieldAnarchyFMessage()
        {
            this.N3MessageType = N3MessageType.PlayfieldAnarchyF;
            this.Unknown = 0x00;
            this.Version = 0x00000004;
            this.TokenMarker = 0x61;
        }

        #endregion

        #region Wire fields

        /// <summary>
        /// 4 in every capture; gates the blocks after the coordinates (&gt; 1 and &gt; 3).
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Where the character lands.
        /// </summary>
        public Vector3 CharacterCoordinates { get; set; }

        /// <summary>
        /// 0x61, a tag the client stamps.
        /// </summary>
        public byte TokenMarker { get; set; }

        /// <summary>
        /// The playfield model (OmniCell ModelId). The SDK reads it as <see cref="PlayfieldId1"/>.
        /// </summary>
        public Identity ModelId { get; set; }

        public int Group { get; set; }

        /// <summary>
        /// OmniCell Subgroup; the AOSharp original's <see cref="SG"/>.
        /// </summary>
        public int Subgroup { get; set; }

        /// <summary>
        /// The playfield instance identity (OmniCell PlayfieldId); the AOSharp original's <see cref="ProxyId"/>.
        /// </summary>
        public Identity PlayfieldId { get; set; }

        /// <summary>
        /// The generator's DbObject identity (empty when the message carries none). Type 51103, 51069 or 51067.
        /// </summary>
        public Identity GeneratorIdentity { get; set; }

        /// <summary>
        /// The generator row's revision (the int after its identity); 0 when there is no generator.
        /// </summary>
        public int GeneratorRevision { get; set; }

        /// <summary>
        /// The generated building (identity type 51103): the mission layout - grid size, template playfield and the
        /// placed rooms. Null when the playfield is not a generated building.
        /// </summary>
        public BuildingGeneratorData Generator { get; set; }

        /// <summary>
        /// A static playfield's dynel runs (identity type 51069). Null otherwise.
        /// </summary>
        public PlayfieldTemplateGeneratorData TemplateGenerator { get; set; }

        /// <summary>
        /// An owned building (identity type 51067). Null otherwise.
        /// </summary>
        public OwnedBuildingGeneratorData OwnedBuildingGenerator { get; set; }

        /// <summary>
        /// The raw generator bytes when the identity type is none of the three known ones (never captured); the
        /// reader then takes everything up to the final PlayfieldX/PlayfieldZ as this block instead of failing the
        /// whole zone-in.
        /// </summary>
        public byte[] UnknownGeneratorData { get; set; }

        /// <summary>
        /// The trailing int32 (-1 in every mission capture).
        /// </summary>
        public int PlayfieldX { get; set; }

        /// <summary>
        /// The trailing int32 (-1 in every mission capture).
        /// </summary>
        public int PlayfieldZ { get; set; }

        #endregion

        #region Compatibility aliases

        /// <summary>
        /// Alias of <see cref="ModelId"/> (the AOSharp original's name; Playfield.Init reads it).
        /// </summary>
        public Identity PlayfieldId1 { get => this.ModelId; set => this.ModelId = value; }

        /// <summary>
        /// Alias of <see cref="Subgroup"/>.
        /// </summary>
        public int SG { get => this.Subgroup; set => this.Subgroup = value; }

        /// <summary>
        /// Alias of <see cref="PlayfieldId"/>.
        /// </summary>
        public Identity ProxyId { get => this.PlayfieldId; set => this.PlayfieldId = value; }

        /// <summary>
        /// The generator identity's type (the AOSharp original's name).
        /// </summary>
        public int UnknownIdType => (int)this.GeneratorIdentity.Type;

        /// <summary>
        /// The generator identity's instance (the AOSharp original's name).
        /// </summary>
        public int UnknownIdInstance => this.GeneratorIdentity.Instance;

        /// <summary>
        /// The AOSharp original's flat view of an owned building (identity type 51067), rebuilt from
        /// <see cref="OwnedBuildingGenerator"/>; null otherwise.
        /// </summary>
        public UnknownStruct1 Unknown7
        {
            get
            {
                var o = this.OwnedBuildingGenerator;
                if (o == null)
                {
                    return null;
                }

                return new UnknownStruct1
                       {
                           Version = o.Unknown1,
                           Unknown2 = o.Model,
                           Unknown3 = o.EntranceDoor,
                           Unknown4 = o.Position,
                           Group = o.Marker,
                           Subgroup = o.SecondListCount,
                           Unknown7 = System.BitConverter.Int32BitsToSingle(o.Unknown3),
                           Unknown8 = o.Unknown4,
                           Unknown9 = o.Unknown5
                       };
            }
        }

        /// <summary>
        /// The live-instance runs of a static playfield (template generator) or owned building, one record per run
        /// (per placement for an owned building): IdentityType, Unknown1 (the run's extra word, or the owned run's
        /// placement count), Unknown2 = start index, Unknown3 = count, Instance = first instance. Null when the
        /// message carries neither. Playfield.LiveIdentity walks these.
        /// </summary>
        public PlayfieldDynelInfo[] Dynels
        {
            get
            {
                if (this.TemplateGenerator != null)
                {
                    var runs = this.TemplateGenerator.Runs ?? new PlayfieldDynelRun[0];
                    var result = new PlayfieldDynelInfo[runs.Length];
                    for (var i = 0; i < runs.Length; i++)
                    {
                        result[i] = new PlayfieldDynelInfo
                                    {
                                        IdentityType = runs[i].Type,
                                        Unknown1 = runs[i].Unknown,
                                        Unknown2 = runs[i].StartIndex,
                                        Unknown3 = runs[i].Count,
                                        Instance = runs[i].FirstInstance
                                    };
                    }

                    return result;
                }

                if (this.OwnedBuildingGenerator != null)
                {
                    var list = new System.Collections.Generic.List<PlayfieldDynelInfo>();
                    foreach (var run in this.OwnedBuildingGenerator.Runs ?? new OwnedBuildingDynelRun[0])
                    {
                        foreach (var p in run.Placements ?? new OwnedBuildingPlacement[0])
                        {
                            list.Add(new PlayfieldDynelInfo
                                     {
                                         IdentityType = run.Type,
                                         Unknown1 = run.Placements.Length,
                                         Unknown2 = p.StartIndex,
                                         Unknown3 = p.Count,
                                         Instance = p.FirstInstance
                                     });
                        }
                    }

                    return list.ToArray();
                }

                return null;
            }
        }

        #endregion

        #region Nested types

        /// <summary>
        /// The recipe for a generated building - a mission (OmniCell GameData\BuildingGeneratorData.cs). Doors'
        /// Room/AdjoiningRoom (DoorFullUpdate) index into <see cref="Rooms"/>.
        /// </summary>
        public class BuildingGeneratorData
        {
            /// <summary>The DbObject identity (type 51103, the mission instance).</summary>
            public Identity Identity { get; set; }

            /// <summary>The row revision.</summary>
            public int Revision { get; set; }

            /// <summary>3; the client accepts nothing else.</summary>
            public short Version { get; set; }

            /// <summary>Grid width in cells (a cell is 10 world units); 30 in the captures.</summary>
            public short Width { get; set; }

            /// <summary>Grid depth in cells; 30 in the captures.</summary>
            public short Height { get; set; }

            /// <summary>Building height in world units; 64 in the captures.</summary>
            public short WorldHeight { get; set; }

            /// <summary>The playfield the mission is generated from ("template pf"): 320/321/324/341/346/351 seen.</summary>
            public int TemplatePlayfield { get; set; }

            public byte AmbientRed { get; set; }

            public byte AmbientGreen { get; set; }

            public byte AmbientBlue { get; set; }

            /// <summary>The placed rooms (int32-counted, 6 bytes each); 7-33 per mission in the recordings.</summary>
            public BuildingRoomInfo[] Rooms { get; set; }
        }

        /// <summary>
        /// One room of a generated building (OmniCell GameData\BuildingRoomInfo.cs; client BuildingRoomInfo_t).
        /// </summary>
        public class BuildingRoomInfo
        {
            /// <summary>Which room template to place (&lt;= 10,000).</summary>
            public short Room { get; set; }

            /// <summary>The floor, signed, -16 to 16.</summary>
            public sbyte Floor { get; set; }

            /// <summary>Grid column; world X = X * 10.</summary>
            public byte X { get; set; }

            /// <summary>Grid row; world Z = playfield extent - Z * 10.</summary>
            public byte Z { get; set; }

            /// <summary>Quarter turns, 0-3.</summary>
            public byte Rotation { get; set; }
        }

        /// <summary>
        /// A static playfield's generator (identity type 51069): runs of live dynel instances.
        /// </summary>
        public class PlayfieldTemplateGeneratorData
        {
            public Identity Identity { get; set; }

            public int Revision { get; set; }

            public int Version { get; set; }

            /// <summary>Int32-counted, 20 bytes each.</summary>
            public PlayfieldDynelRun[] Runs { get; set; }
        }

        /// <summary>
        /// One run of a template generator: dynels of one type, numbered from FirstInstance.
        /// </summary>
        public class PlayfieldDynelRun
        {
            public IdentityType Type { get; set; }

            public int Unknown { get; set; }

            public int StartIndex { get; set; }

            public int Count { get; set; }

            public int FirstInstance { get; set; }
        }

        /// <summary>
        /// An owned building's generator (identity type 51067), per OmniCell's OwnedBuildingGeneratorData.
        /// </summary>
        public class OwnedBuildingGeneratorData
        {
            public Identity Identity { get; set; }

            public int Revision { get; set; }

            public int Version { get; set; }

            public int Unknown1 { get; set; }

            public Identity Model { get; set; }

            public int EntranceDoor { get; set; }

            public Vector3 Position { get; set; }

            public int Marker { get; set; }

            /// <summary>
            /// The count of a second record list no capture has ever shown non-empty; its record length is unknown,
            /// so when it is non-zero the reader keeps the rest of the generator raw in <see cref="UnreadTail"/>.
            /// </summary>
            public int SecondListCount { get; set; }

            /// <summary>
            /// The raw rest of the generator when <see cref="SecondListCount"/> is non-zero; null otherwise.
            /// </summary>
            public byte[] UnreadTail { get; set; }

            public int Unknown3 { get; set; }

            public int Unknown4 { get; set; }

            public int Unknown5 { get; set; }

            /// <summary>Int32-counted runs, each with its own int32-counted placements.</summary>
            public OwnedBuildingDynelRun[] Runs { get; set; }
        }

        /// <summary>
        /// One run of an owned building: a dynel type and its placements.
        /// </summary>
        public class OwnedBuildingDynelRun
        {
            public IdentityType Type { get; set; }

            public OwnedBuildingPlacement[] Placements { get; set; }
        }

        /// <summary>
        /// One placement in an owned-building run.
        /// </summary>
        public class OwnedBuildingPlacement
        {
            public int StartIndex { get; set; }

            public int Count { get; set; }

            public int FirstInstance { get; set; }
        }

        /// <summary>
        /// The AOSharp original's flat view of an owned building; kept for compatibility (see <see cref="Unknown7"/>).
        /// </summary>
        public class UnknownStruct1
        {
            [AoMember(0)]
            public int Version { get; set; }
            [AoMember(1)]
            public Identity Unknown2 { get; set; }
            [AoMember(2)]
            public int Unknown3 { get; set; }
            [AoMember(3)]
            public Vector3 Unknown4 { get; set; }
            [AoMember(4)]
            public int Group { get; set; }
            [AoMember(5)]
            public int Subgroup { get; set; }
            [AoMember(6)]
            public float Unknown7 { get; set; }
            [AoMember(7)]
            public int Unknown8 { get; set; }
            [AoMember(8)]
            public int Unknown9 { get; set; }
        }

        /// <summary>
        /// The AOSharp original's run record; see <see cref="Dynels"/>.
        /// </summary>
        public class PlayfieldDynelInfo
        {
            [AoMember(0)]
            public IdentityType IdentityType { get; set; }
            [AoMember(1)]
            public int Unknown1 { get; set; }
            [AoMember(2)]
            public int Unknown2 { get; set; }
            [AoMember(3)]
            public int Unknown3 { get; set; }
            [AoMember(4)]
            public int Instance { get; set; }
        }

        #endregion
    }
}
