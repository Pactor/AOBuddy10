using AOSharp.Common.GameData;
using System;

namespace AOSharp.Clientless
{
    public class Transform
    {
        private Vector3 _pos, _to;
        private float _speed;
        private DateTime _at;
        private bool _moving;

        /// <summary>
        /// Where the dynel is now. A dynel on a server path (FollowTarget) is moved along it from where the
        /// packet put it toward its end at its speed, stopping there; setting Position ends the path.
        /// </summary>
        public Vector3 Position
        {
            get
            {
                if (!_moving) return _pos;
                var d = _to - _pos;
                float len = d.Magnitude, run = (float)(DateTime.UtcNow - _at).TotalSeconds * _speed;
                return run >= len || len < 0.001f ? _to : _pos + d * (run / len);
            }
            set { _pos = value; _moving = false; }
        }

        public Quaternion Heading;

        /// <summary>True while the dynel is on a server path and short of its end.</summary>
        public bool Moving => _moving && (DateTime.UtcNow - _at).TotalSeconds * _speed < (_to - _pos).Magnitude;

        /// <summary>The end of the current server path, or null when not on one.</summary>
        public Vector3? Destination => _moving ? _to : (Vector3?)null;

        internal void MoveAlong(Vector3 from, Vector3 to, float speed)
        {
            _pos = from; _to = to; _speed = speed; _at = DateTime.UtcNow; _moving = speed > 0;
        }
    }
}
