using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOSharp.Clientless
{
    public class Dynel : StatHolder
    {
        public string Name { get; internal set; }
        public readonly Identity Identity;
        public Transform Transform { get; private set; }

        public Dynel(Identity identity, Vector3 position, Quaternion heading) : this(identity)
        {
            InitTransform(position, heading);
        }

        public Dynel(Identity identity)
        {
            Identity = identity;
        }

        protected void InitTransform(Vector3 position, Quaternion heading)
        {
            Transform = new Transform
            {
                Position = position,
                Heading = heading,
            };
        }

        public void Use()
        {
            Client.Send(new GenericCmdMessage()
            {
                Action = GenericCmdAction.Use,
                User = DynelManager.LocalPlayer.Identity,
                Target = Identity,
                Count = 1,
                Temp4 = 1,
            });
        }

        public float DistanceFrom(Vector3 pos) => Vector3.Distance(Transform.Position, pos);

        public float DistanceFrom(Dynel dynel) => DistanceFrom(dynel.Transform.Position);
    }
}
