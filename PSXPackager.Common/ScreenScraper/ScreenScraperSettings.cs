using System;
using System.IO;
using System.Text.Json;

namespace PSXPackager.Common.ScreenScraper
{
    public class ScreenScraperSettings
    {
        public string DevId { get; set; } = "";
        public string DevPassword { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool AutoDownloadArtwork { get; set; } = true;
        public bool UseInBatchMode { get; set; } = true;

        private static string GetSettingsFilePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "PSXPackagerPlus");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "screenscraper-settings.json");
        }

        public static string GetArtworkCacheDirectory()
        {
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var artworkDir = Path.Combine(exeDir, "ScreenScraperArtwork");
            Directory.CreateDirectory(artworkDir);
            return artworkDir;
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetSettingsFilePath(), json);
        }

        public static ScreenScraperSettings Load()
        {
            var filePath = GetSettingsFilePath();
            if (!File.Exists(filePath))
                return new ScreenScraperSettings();

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<ScreenScraperSettings>(json) ?? new ScreenScraperSettings();
            }
            catch
            {
                return new ScreenScraperSettings();
            }
        }
    }
}