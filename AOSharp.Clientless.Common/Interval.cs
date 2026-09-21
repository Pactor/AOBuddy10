using System;

namespace AOSharp.Clientless
{
    public class Interval
    {
        private DateTime _nextExecuteTime;
        private TimeSpan _interval;

        public Interval(int ms)
        {
            _interval = TimeSpan.FromMilliseconds(ms);
            Reset();
        }

        public virtual bool Elapsed => DateTime.Now >= _nextExecuteTime;

        public void ExecuteIfElapsed(Action action)
        {
            if (Elapsed)
            {
                Reset();
                action?.Invoke();
            }
        }

        public void Reset() => _nextExecuteTime = DateTime.Now.Add(_interval);
    }

    public class AutoResetInterval : Interval
    {
        public AutoResetInterval(int ms) : base(ms) { }

        public override bool Elapsed => GetAndResetIfElapsed();

        private bool GetAndResetIfElapsed()
        {
            if (base.Elapsed)
            {
                Reset();
                return true;
            }

            return false;
        }
    }
}