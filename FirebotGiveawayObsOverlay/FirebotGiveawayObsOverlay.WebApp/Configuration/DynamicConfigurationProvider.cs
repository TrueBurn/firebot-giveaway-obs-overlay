using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FirebotGiveawayObsOverlay.WebApp.Configuration;

public class DynamicConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly string _jsonFilePath;
    private readonly FileSystemWatcher _fileWatcher;
    private readonly object _lock = new();
    private bool _disposed;

    public DynamicConfigurationProvider(string jsonFilePath)
    {
        _jsonFilePath = jsonFilePath;
        
        // Set up file watcher to detect external changes
        _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(jsonFilePath)!)
        {
            Filter = Path.GetFileName(jsonFilePath),
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.LastWrite
        };
        
        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.Error += OnWatcherError;
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        // Log the error
        Console.WriteLine($"File watcher error: {e.GetException().Message}");
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Avoid duplicate events by using a small delay
        lock (_lock)
        {
            if (!_disposed)
            {
                // Small delay to ensure the file is not locked
                Thread.Sleep(100);
                Load();
            }
        }
    }

    public override void Load()
    {
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        if (File.Exists(_jsonFilePath))
        {
            try
            {
                // Use a retry mechanism to handle file locks
                int retryCount = 0;
                bool success = false;
                
                while (!success && retryCount < 3)
                {
                    try
                    {
                        using var stream = new FileStream(_jsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var jsonDoc = JsonDocument.Parse(stream);
                        
                        FlattenJson(jsonDoc.RootElement, string.Empty, data);
                        success = true;
                    }
                    catch (IOException)
                    {
                        retryCount++;
                        Thread.Sleep(100 * retryCount);
                    }
                }
                
                if (!success)
                {
                    Console.WriteLine($"Failed to load configuration from {_jsonFilePath} after multiple attempts");
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing JSON from {_jsonFilePath}: {ex.Message}");
            }
        }
        
        Data = data;
        OnReload();
    }
    
    private void FlattenJson(JsonElement element, string path, Dictionary<string, string> data)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyPath = string.IsNullOrEmpty(path)
                        ? property.Name
                        : $"{path}:{property.Name}";
                    
                    FlattenJson(property.Value, propertyPath, data);
                }
                break;
                
            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var itemPath = $"{path}:{index}";
                    FlattenJson(item, itemPath, data);
                    index++;
                }
                break;
                
            case JsonValueKind.String:
                data[path] = element.GetString() ?? string.Empty;
                break;
                
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                data[path] = element.ToString();
                break;
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _fileWatcher.Changed -= OnFileChanged;
            _fileWatcher.Error -= OnWatcherError;
            _fileWatcher.Dispose();
        }
    }
}

public class DynamicConfigurationSource : IConfigurationSource
{
    private readonly string _jsonFilePath;
    
    public DynamicConfigurationSource(string jsonFilePath)
    {
        _jsonFilePath = jsonFilePath;
    }
    
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DynamicConfigurationProvider(_jsonFilePath);
    }
}

public static class DynamicConfigurationExtensions
{
    public static IConfigurationBuilder AddDynamicJsonFile(
        this IConfigurationBuilder builder, 
        string path)
    {
        return builder.Add(new DynamicConfigurationSource(path));
    }
}