using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace AOSharp.Common.SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    [AoContract((int)N3MessageType.Mail)]
    public class MailMessage : N3Message
    {
        public MailMessage()
        {
            this.N3MessageType = N3MessageType.Mail;
        }

        [AoMember(0)]
        public short Unknown1 { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Recipient { get; set; }

        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string Subject { get; set; }

        [AoMember(3, SerializeSize = ArraySizeType.Int16)]
        public string Body { get; set; }

        [AoMember(4)]
        public Identity Item { get; set; }

        [AoMember(5)]
        public int Credits { get; set; }

        [AoMember(6)]
        public bool Express { get; set; }
    }
}
