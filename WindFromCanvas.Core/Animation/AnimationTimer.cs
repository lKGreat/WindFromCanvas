using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace WindFromCanvas.Core.Animation
{
    public class AnimationTimer
    {
        private readonly Timer _timer;
        private readonly Stopwatch _stopwatch;
        private DateTime _lastFrameTime;

        public event Action<double> OnFrame;  // 参数为 deltaTime（毫秒）
        public int TargetFPS { get; set; } = 60;
        public bool IsRunning => _timer.Enabled;

        public AnimationTimer()
        {
            _timer = new Timer();
            _timer.Tick += Timer_Tick;
            _stopwatch = new Stopwatch();
            _lastFrameTime = DateTime.Now;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var currentTime = DateTime.Now;
            var deltaTime = (currentTime - _lastFrameTime).TotalMilliseconds;
            _lastFrameTime = currentTime;

            OnFrame?.Invoke(deltaTime);
        }

        public void Start()
        {
            if (!_timer.Enabled)
            {
                int interval = (int)(1000.0 / TargetFPS);
                _timer.Interval = Math.Max(1, interval);
                _lastFrameTime = DateTime.Now;
                _stopwatch.Start();
                _timer.Start();
            }
        }

        public void Stop()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
                _stopwatch.Stop();
            }
        }
    }
}
