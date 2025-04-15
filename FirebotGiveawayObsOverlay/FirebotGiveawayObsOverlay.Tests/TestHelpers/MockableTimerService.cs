using FirebotGiveawayObsOverlay.WebApp.Services;

namespace FirebotGiveawayObsOverlay.Tests.TestHelpers;

/// <summary>
/// A mockable version of TimerService for testing
/// </summary>
public class MockableTimerService : TimerService
{
    private bool _timerWasReset;

    public bool TimerWasReset => _timerWasReset;

    public override void ResetTimer()
    {
        _timerWasReset = true;
        base.ResetTimer();
    }

    public void Reset()
    {
        _timerWasReset = false;
    }
}