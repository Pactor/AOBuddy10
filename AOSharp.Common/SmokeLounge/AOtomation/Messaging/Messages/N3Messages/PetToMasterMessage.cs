using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A pet attaching to, or detaching from, its master. Operation 1 is the attach and carries the
    /// pet's type in AttachNotificationValue - the same number SimpleNpcInfo.PetType gives (10
    /// attack, 11 heal, 12 mezz); operation 2 is the detach and carries zero.
    /// Wire-verified (captures/mp_203534_s37.csv: Operation=1 AttachNotificationValue=10 then
    /// Operation=2 AttachNotificationValue=0, for each of the master's pets).
    /// </summary>
    [AoContract((int)N3MessageType.PetToMaster)]
    public class PetToMasterMessage : N3Message
    {
        public PetToMasterMessage()
        {
            this.N3MessageType = N3MessageType.PetToMaster;
        }

        [AoMember(1)]
        public Identity PetIdentity { get; set; }

        [AoMember(2)]
        public int Operation { get; set; }

        [AoMember(3)]
        public int AttachNotificationValue { get; set; }

        [AoMember(4)]
        public Identity Unread { get; set; }
    }
}
