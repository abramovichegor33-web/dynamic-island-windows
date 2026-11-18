using System.IO;
using System.Text.Json;
using DynamicIsland.Models;

namespace DynamicIsland.Services;

public class ConfigService
{
    private static readonly Lazy<ConfigService> _instance = new(() => new ConfigService());
    public static ConfigService Instance => _instance.Value;

    private readonly string _configFilePath;
    private AppConfig _config;

    public AppConfig Config => _config;

    public event Action<AppConfig>? ConfigChanged;

    private ConfigService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "DynamicIslandWindows");
        Directory.CreateDirectory(dir);
        _configFilePath = Path.Combine(dir, "config.json");

        _config = Load();
    }

    private AppConfig Load()
    {
        if (File.Exists(_configFilePath))
        {
            try
            {
                var json = File.ReadAllText(_configFilePath);
                var loaded = JsonSerializer.Deserialize<AppConfig>(json);
                if (loaded != null) return loaded;
            }
            catch
            {
                // corrupted file fallback
            }
        }

        var def = new AppConfig();
        Save(def);
        return def;
    }

    public void Save(AppConfig? config = null)
    {
        if (config != null) _config = config;

        try
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFilePath, json);
            ConfigChanged?.Invoke(_config);
        }
        catch
        {
            // ignore disk errors
        }
    }
}
