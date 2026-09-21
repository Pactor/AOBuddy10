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

        #endregion
    }
}