using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Clientless
{
    public class LocalPlayerMovementComponent : MovementComponent
    {
        public override void ChangeMovement(MovementAction action)
        {
            if (action == MovementAction.LeaveSit)
            {
                Client.Send(new CharacterActionMessage()
                {
                    Action = CharacterActionType.StandUp
                });
            }
            else
            {
                Client.Send(new CharDCMoveMessage()
                {
                    Position = Position,
                    Heading = Heading,
                    MoveType = action
                });
            }

            base.ChangeMovement(action);
        }
    }
}
