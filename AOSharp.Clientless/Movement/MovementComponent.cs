using AOSharp.Common.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Clientless
{
    public class MovementComponent : Transform
    {
        public MovementFlags Flags;
        public bool IsTurning => Flags.HasFlag(MovementFlags.TurningLeft) || Flags.HasFlag(MovementFlags.TurningRight);

        public virtual void ChangeMovement(MovementAction action)
        {
            SetFlags(action);
        }

        public void SetFlags(MovementAction action)
        {
            if (action == MovementAction.ForwardStart)
                Flags |= MovementFlags.Forward;
            else if (action == MovementAction.ForwardStop)
                Flags &= ~MovementFlags.Forward;
            else if (action == MovementAction.BackwardStart)
                Flags |= MovementFlags.Backward;
            else if (action == MovementAction.BackwardStop)
                Flags &= ~MovementFlags.Backward;
            else if (action == MovementAction.StrafeLeftStart)
                Flags |= MovementFlags.StrafeLeft;
            else if (action == MovementAction.StrafeLeftStop)
                Flags &= ~MovementFlags.StrafeLeft;
            else if (action == MovementAction.StrafeRightStart)
                Flags |= MovementFlags.StrafeRight;
            else if (action == MovementAction.StrafeRightStop)
                Flags &= ~MovementFlags.StrafeRight;
            else if (action == MovementAction.TurnLeftStart)
                Flags |= MovementFlags.TurningLeft;
            else if (action == MovementAction.TurnLeftStop)
                Flags &= ~MovementFlags.TurningLeft;
            else if (action == MovementAction.TurnRightStart)
                Flags |= MovementFlags.TurningRight;
            else if (action == MovementAction.TurnRightStop)
                Flags &= ~MovementFlags.TurningRight;
            else if (action == MovementAction.FullStop)
                Flags = MovementFlags.None;
        }
    }

    [Flags]
    public enum MovementFlags
    {
        None = 0,
        Forward = 1,
        Backward = 2,
        StrafeLeft = 4,
        StrafeRight = 8,
        TurningLeft = 16,
        TurningRight = 32
    }
}
