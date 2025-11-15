using System;
using System.IO;
using Newtonsoft.Json;

namespace Stalker2LocalizationManager
{
    public class AppConfig
    {
        public string? SourceFile { get; set; }
        public string? TargetFile { get; set; }
        public string? SelectedLanguage { get; set; }
        public string? SelectedProvider { get; set; } // "LibreTranslate", "MyMemory", "Google"

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Stalker2LocalizationManager",
            "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    System.Diagnostics.Debug.WriteLine($"Config file content: {json}");
                    var config = JsonConvert.DeserializeObject<AppConfig>(json);
                    if (config != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Config loaded - SourceFile: {config.SourceFile}, TargetFile: {config.TargetFile}");
                        return config;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Config file not found at: {ConfigPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
            }

            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
                System.Diagnostics.Debug.WriteLine($"Config saved to: {ConfigPath}");
                System.Diagnostics.Debug.WriteLine($"Saved config - SourceFile: {SourceFile}, TargetFile: {TargetFile}, Language: {SelectedLanguage}, Provider: {SelectedProvider}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }
    }
}

