namespace FirebotGiveawayObsOverlay.Tests;

internal sealed class TempFolder : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("firebot-overlay-tests-").FullName;

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Write(string name, string content)
    {
        var path = File(name);
        System.IO.File.WriteAllText(path, content);
        // Ensure change detection sees a new timestamp even on coarse-resolution file systems
        System.IO.File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddTicks(Random.Shared.Next(1, 1_000_000)));
    }

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch { /* best effort */ }
    }
}
