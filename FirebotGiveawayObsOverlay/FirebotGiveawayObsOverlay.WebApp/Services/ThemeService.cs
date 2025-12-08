namespace FirebotGiveawayObsOverlay.WebApp.Services;

public class ThemeService
{
    public event Action? OnThemeChanged;

    public void NotifyThemeChanged()
    {
        OnThemeChanged?.Invoke();
    }
}
