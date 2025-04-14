using System;

namespace FirebotGiveawayObsOverlay.Tests
{
    /// <summary>
    /// Mock implementation of TimerService for testing
    /// </summary>
    public class MockTimerService
    {
        public event EventHandler? TimerElapsed;
        
        private bool _isRunning;
        private TimeSpan _interval;
        private TimeSpan _elapsed;

        public MockTimerService()
        {
            _isRunning = false;
            _interval = TimeSpan.FromMinutes(1);
            _elapsed = TimeSpan.Zero;
        }

        public void Start()
        {
            _isRunning = true;
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void ResetTimer()
        {
            _elapsed = TimeSpan.Zero;
        }

        public bool IsRunning => _isRunning;
        
        public TimeSpan Elapsed => _elapsed;
        
        public TimeSpan Interval
        {
            get => _interval;
            set => _interval = value;
        }

        // Method to simulate timer elapsed for testing
        public void SimulateTimerElapsed()
        {
            TimerElapsed?.Invoke(this, EventArgs.Empty);
        }
    }
}