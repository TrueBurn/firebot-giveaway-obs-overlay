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

        try
        {
            await foreach (var settings in _persistenceService.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await _userSettingsService.SaveUserSettingsAsync(settings, stoppingToken);
                    _logger.LogDebug("Settings saved to disk");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Application is shutting down, try one final save
                    _logger.LogInformation("Shutdown requested, performing final save");
                    try
                    {
                        await _userSettingsService.SaveUserSettingsAsync(settings, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save settings during shutdown");
                    }
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save settings to disk");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Background settings writer service stopping");
        }

        _logger.LogInformation("Background settings writer service stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background settings writer service stopping, flushing pending settings");

        // Flush any pending settings to the channel before stopping
        _persistenceService.Flush();

        await base.StopAsync(cancellationToken);
    }
}
