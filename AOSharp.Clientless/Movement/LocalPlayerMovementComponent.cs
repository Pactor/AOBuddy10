using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Clientless
{
    public class LocalPlayerMovementComponent : MovementComponent
    {
        // DeltaTime since the previous movement packet, as the real client sends it: a capture of a live
        // client walking (20260925-141138) carries 9-29 ms between moves, and the server reads the field to
        // judge how far a step may claim. It was never set here — every packet said 0 ms, i.e. infinite
        // implied speed — and the server rubberbanded the bot back after ~8 m of "instant" progress
        // (Wailing Wastes 2026-09-25, the easy-slope rubberbanding).
        private readonly Stopwatch _sinceLastMove = Stopwatch.StartNew();

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
                    MoveType = action,
                    DeltaTime = (int)Math.Min(int.MaxValue, _sinceLastMove.ElapsedMilliseconds)
                });
                _sinceLastMove.Restart();
            }

            base.ChangeMovement(action);
        }
    }
}