using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The counterpart to <see cref="AddPetMessage"/>: this pet is no longer ours (died, was
    /// dismissed, or we zoned). Wire-verified (sniffs/20260913-154958_s11.csv seq 5342).
    /// </summary>
    [AoContract((int)N3MessageType.RemovePet)]
    public class RemovePetMessage : N3Message
    {
        public RemovePetMessage()
        {
            this.N3MessageType = N3MessageType.RemovePet;
        }

        [AoMember(1)]
        public Identity PetIdentity { get; set; }
    }
}
