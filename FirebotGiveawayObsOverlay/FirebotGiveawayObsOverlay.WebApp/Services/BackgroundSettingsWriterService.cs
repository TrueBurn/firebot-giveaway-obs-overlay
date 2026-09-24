namespace FirebotGiveawayObsOverlay.WebApp.Services;

/// <summary>
/// Background service that consumes settings from the persistence channel
/// and writes them to disk asynchronously.
/// </summary>
public class BackgroundSettingsWriterService : BackgroundService
{
    private readonly SettingsPersistenceService _persistenceService;
    private readonly UserSettingsService _userSettingsService;
    private readonly ILogger<BackgroundSettingsWriterService> _logger;

    public BackgroundSettingsWriterService(
        SettingsPersistenceService persistenceService,
        UserSettingsService userSettingsService,
        ILogger<BackgroundSettingsWriterService> logger)
    {
        _persistenceService = persistenceService;
        _userSettingsService = userSettingsService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background settings writer service started");

        // Deliberately not passing stoppingToken to ReadAllAsync: on shutdown StopAsync completes the channel,
        // and we want to drain (write) whatever was flushed rather than abandon it.
        await foreach (var settings in _persistenceService.Reader.ReadAllAsync(CancellationToken.None))
        {
            try
            {
                await _userSettingsService.SaveUserSettingsAsync(settings, CancellationToken.None);
                _logger.LogDebug("Settings saved to disk");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings to disk");
            }
        }

        _logger.LogInformation("Background settings writer service stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background settings writer service stopping, flushing pending settings");

        // Flush pending settings and complete the channel so ExecuteAsync drains and exits
        _persistenceService.Complete();

        await base.StopAsync(cancellationToken);
    }
}
