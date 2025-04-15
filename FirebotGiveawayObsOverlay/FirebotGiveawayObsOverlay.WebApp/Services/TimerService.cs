using System;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

public class TimerService
{
    public event Action? OnTimerReset;

    public virtual void ResetTimer()
    {
        OnTimerReset?.Invoke();
    }
} 