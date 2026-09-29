// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GfxAttachment.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the GfxAttachment type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (GameData\GfxAttachment.cs); no changes.

    /// <summary>
    /// Where on a character an effect hangs.
    /// </summary>
    /// <remarks>
    /// The names are Funcom's. _GfxLocator_t's lookup at Gamecode 0x10105E6D
    /// takes this number and returns a matrix to hang the effect off, and for
    /// anything between 1000 and 1018 it subtracts 1000, indexes the table of
    /// nineteen strings at 0x102C63A8, and hands the string it finds to
    /// RCATMesh_t::GetBoneMatrix - so the table is the enumeration and the
    /// strings below are the strings in it. When a bone is not found it falls
    /// back on RCATMesh_t::GetAttractor with "Attractor01_head".
    ///
    /// Two values are not bones. 3000 casts the target to a WeaponItem_t and
    /// takes its frame; 3001 asks the target's VisualMesh_t for its attribute
    /// matrix. Both are left alone by the caller at 0x1010674E, which combines
    /// every other value with the dynel's world matrix and leaves those two as
    /// they are. Zero means nowhere in particular and the lookup is skipped.
    /// </remarks>
    public enum GfxAttachment
    {
        /// <summary>
        /// Nowhere in particular. The lookup at 0x10106720 is skipped.
        /// </summary>
        None = 0,

        /// <summary>
        /// "Bip01 Pelvis_ac".
        /// </summary>
        Pelvis = 1000,

        /// <summary>
        /// "Bip01 Spine_ac".
        /// </summary>
        Spine = 1001,

        /// <summary>
        /// "Bip01 Spine1_ac".
        /// </summary>
        Spine1 = 1002,

        /// <summary>
        /// "Bip01 Spine2_ac".
        /// </summary>
        Spine2 = 1003,

        /// <summary>
        /// "Bip01 Spine3_ac".
        /// </summary>
        Spine3 = 1004,

        /// <summary>
        /// "Bip01 Neck_ac".
        /// </summary>
        Neck = 1005,

        /// <summary>
        /// "Bip01 Head_ac".
        /// </summary>
        Head = 1006,

        /// <summary>
        /// "Bip01 L UpperArm_ac".
        /// </summary>
        LeftUpperArm = 1007,

        /// <summary>
        /// "Bip01 R UpperArm_ac".
        /// </summary>
        RightUpperArm = 1008,

        /// <summary>
        /// "Bip01 L Forearm_ac".
        /// </summary>
        LeftForearm = 1009,

        /// <summary>
        /// "Bip01 R Forearm_ac".
        /// </summary>
        RightForearm = 1010,

        /// <summary>
        /// "Bip01 L Thigh_ac".
        /// </summary>
        LeftThigh = 1011,

        /// <summary>
        /// "Bip01 R Thigh_ac".
        /// </summary>
        RightThigh = 1012,

        /// <summary>
        /// "Bip01 L Calf_ac".
        /// </summary>
        LeftCalf = 1013,

        /// <summary>
        /// "Bip01 R Calf_ac".
        /// </summary>
        RightCalf = 1014,

        /// <summary>
        /// "Bip01 L Foot_ac".
        /// </summary>
        LeftFoot = 1015,

        /// <summary>
        /// "Bip01 R Foot_ac".
        /// </summary>
        RightFoot = 1016,

        /// <summary>
        /// "Bip01 L Hand_ac".
        /// </summary>
        LeftHand = 1017,

        /// <summary>
        /// "Bip01 R Hand_ac".
        /// </summary>
        RightHand = 1018,

        /// <summary>
        /// The weapon's own frame. The lookup casts the target to a
        /// WeaponItem_t rather than to a character.
        /// </summary>
        Weapon = 3000,

        /// <summary>
        /// The target mesh's own attribute matrix, through
        /// VisualMesh_t::GetAttrMatrix.
        /// </summary>
        MeshAttribute = 3001
    }
}
