using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The server telling us a pet now belongs to us. Sent once per pet as it appears, straight
    /// after that pet's SimpleCharFullUpdate - so it is the exact moment a summon landed, rather
    /// than something to notice by polling the dynel list.
    /// Wire-verified (sniffs/20260913-154958_s11.csv seq 641; captures/mp_203534_s37.csv seq 97/113/124,
    /// three pets for one master).
    /// </summary>
    [AoContract((int)N3MessageType.AddPet)]
    public class AddPetMessage : N3Message
    {
        public AddPetMessage()
        {
            this.N3MessageType = N3MessageType.AddPet;
        }

        [AoMember(1)]
        public Identity PetIdentity { get; set; }
    }
}
