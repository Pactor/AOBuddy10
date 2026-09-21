using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AOSharp.Clientless.Common
{
    public class UpdateLoop
    {
        public const int UpdateRate = 64;

        private Stopwatch _stopWatch;
        private CancellationTokenSource _cancellationToken;
        private Action<double> _callback;

        public UpdateLoop(Action<double> callback)
        {
            _callback = callback;
        }

        private void Run()
        {
            int desiredDeltaTime = 1000 / UpdateRate;

            while (!_cancellationToken.IsCancellationRequested)
            {
                long deltaTime = _stopWatch.ElapsedMilliseconds;
                _stopWatch.Restart();
                Tick(deltaTime / 1000d);
                Thread.Sleep((int)Math.Max(desiredDeltaTime - _stopWatch.ElapsedMilliseconds, 0));
            }
        }

        private void Tick(double deltaTime)
        {
            _callback.Invoke(deltaTime);
        }

        public void Start()
        {
            _stopWatch = Stopwatch.StartNew();
            _cancellationToken = new CancellationTokenSource();
            Task.Factory.StartNew(Run, _cancellationToken.Token);
        }

        public void Stop()
        {
            _cancellationToken.Cancel();
        }
    }
}
