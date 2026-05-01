using System;
using System.IO;
using System.Text.Json;
using AvaloniaApplication1.Models;

namespace AvaloniaApplication1.Services;

/// <summary>
/// Loads and saves the startup configuration from a JSON file in the user's application data folder.
/// </summary>
public sealed class StartupConfigService
{
    private const string AppFolderName = "AvaloniaApplication1";
    private const string ConfigFileName = "startup-config.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string ConfigPath { get; }

    public StartupConfigService()
    {
        string configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName);

        ConfigPath = Path.Combine(configDirectory, ConfigFileName);
    }

    /// <summary>
    /// Loads the startup config if it exists, or creates a default one the first time the app runs.
    /// </summary>
    public StartupConfiguration LoadOrCreate()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);

        if (File.Exists(ConfigPath))
        {
            return LoadExistingConfig();
        }

        var config = new StartupConfiguration
        {
            InitialRoot = GetDefaultInitialRootPath()
        };

        Save(config);
        return config;
    }

    /// <summary>
    /// Updates only the initial root value in the saved startup config.
    /// </summary>
    public void SaveInitialRoot(string initialRoot)
    {
        var config = new StartupConfiguration
        {
            InitialRoot = initialRoot
        };

        Save(config);
    }

    private StartupConfiguration LoadExistingConfig()
    {
        string json = File.ReadAllText(ConfigPath);
        StartupConfiguration? config = JsonSerializer.Deserialize<StartupConfiguration>(json, JsonOptions);
        if (config is null)
        {
            throw new InvalidDataException("Startup config file is empty or invalid.");
        }

        return config;
    }

    private void Save(StartupConfiguration config)
    {
        string json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    private static string GetDefaultInitialRootPath()
    {
        // Default to the ExamplesRoot directory for fast startup
        string examplesRoot = Path.Combine(AppContext.BaseDirectory, "ExamplesRoot");
        if (Directory.Exists(examplesRoot))
        {
            return examplesRoot;
        }

        // Fallback to user's home directory if ExamplesRoot doesn't exist
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}

