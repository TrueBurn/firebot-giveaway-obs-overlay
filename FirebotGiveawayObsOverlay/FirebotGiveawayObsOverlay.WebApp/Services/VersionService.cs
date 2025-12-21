using System.Reflection;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

public class VersionService
{
    public string Version { get; }
    public string InformationalVersion { get; }

    public VersionService()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Get FileVersion (most commonly displayed)
        var fileVersion = assembly.GetName().Version;
        Version = fileVersion != null
            ? $"{fileVersion.Major}.{fileVersion.Minor}.{fileVersion.Build}"
            : "0.0.0";

        // Get InformationalVersion for full semver with prerelease
        var infoVersionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        InformationalVersion = infoVersionAttr?.InformationalVersion ?? Version;
    }

    public string GetDisplayVersion()
    {
        // Clean up informational version if it contains git hash (e.g., "1.0.0+abc123")
        var version = InformationalVersion;
        var plusIndex = version.IndexOf('+');
        if (plusIndex > 0)
        {
            version = version.Substring(0, plusIndex);
        }
        return version;
    }
}
