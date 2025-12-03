using System.IO;
using System.Text.Json;

namespace UrlProxy.Models;

public class Settings
{
    public int Port { get; set; } = 3000;
    public string TargetUrl { get; set; } = "http://localhost:5059";
    public string FirewallRuleName { get; set; } = "AAProxyRule";
    public bool UseHttps { get; set; } = true;

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "UrlProxy",
        "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
        }
        catch
        {
            // 忽略錯誤，使用預設值
        }

        return new Settings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // 忽略錯誤
        }
    }
}
