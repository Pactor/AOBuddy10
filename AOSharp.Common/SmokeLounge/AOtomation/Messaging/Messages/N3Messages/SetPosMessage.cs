using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.SetPos)]
    public class SetPosMessage : N3Message
    {
        #region Constructors and Destructors

        public SetPosMessage()
        {
            this.N3MessageType = N3MessageType.SetPos;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public Vector3 Position { get; set; }

        // Members 1-3 ported 2026-09-29 from OmniCell's Messages\N3Messages\SetPosMessage.cs:50-89; the AOSharp
        // original stopped after the position and left these six bytes unread on every SetPos.

        /// <summary>
        /// Whether to move the dynel's last allowed position too (a boolean carried as a byte). 1 in every
        /// captured copy.
        /// </summary>
        /// <remarks>
        /// The client's dispatcher (0x100773A3) calls n3Dynel_t::UpdateLastAllowedPosition with the corrected
        /// coordinates when it is set, which stops the client treating the correction as a rubber-band.
        /// </remarks>
        [AoMember(1)]
        public byte UpdateLastAllowedPosition { get; set; }

        /// <summary>
        /// A crowd-limiting figure to show the player, or 0 for none. 0 in every captured copy.
        /// </summary>
        /// <remarks>
        /// When non-zero and the correction is for the client's own character, the client formats it into the
        /// Feedback_CrowdLimiting string. Nothing else reads it.
        /// </remarks>
        [AoMember(2)]
        public int CrowdLimitingFeedback { get; set; }

        /// <summary>
        /// Whether the character should stop moving once corrected (a boolean carried as a byte).
        /// </summary>
        /// <remarks>
        /// When set the client invokes the same movement-controller stop StopMovingCmd uses. OmniCell's note says
        /// it is zero in every capture; not so for retail: in the AOBuddy mission recordings all 21,639 SetPos
        /// for the bot itself carry 1 ("the server halted you here") and all 4,195 for other dynels carry 0.
        /// </remarks>
        [AoMember(3)]
        public byte StopMoving { get; set; }

        #endregion
    }
}