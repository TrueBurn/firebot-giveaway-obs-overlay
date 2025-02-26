using System;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

public class TimerService
{
    public event Action? OnTimerReset;

    public void ResetTimer()
    {
        OnTimerReset?.Invoke();
    }
} 