using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using PSXPackager.Common.ScreenScraper;
using PSXPackagerGUI.Common;

namespace PSXPackagerGUI.Models
{
    public class ScreenScraperModel : BaseNotifyModel
    {
        private readonly ScreenScraperService _screenScraperService;
        private ScreenScraperSettings _settings;
        private string _devId;
        private string _devPassword;
        private string _username;
        private string _password;
        private string _searchStatus;
        private bool _isSearching;
        private GameInfo _currentGameInfo;
        private string _selectedImageUrl;

        public ScreenScraperModel()
        {
            _screenScraperService = new ScreenScraperService();
            SearchCommand = new RelayCommand(async (param) => await SearchGameAsync(), (param) => CanSearch());
            DownloadImageCommand = new RelayCommand(async (param) => await DownloadImageAsync(param as string), (param) => !string.IsNullOrEmpty(param as string));
            DownloadIcon0Command = new RelayCommand(async (param) => await DownloadIcon0Async(), (param) => !string.IsNullOrEmpty(CurrentGameInfo?.Media?.Icon0Url));
            SaveCredentialsCommand = new RelayCommand((param) => SaveCredentialsToSettings(), (param) => true);
            LoadCredentialsFromSettings();
        }

        public string DevId
        {
            get => _devId;
            set
            {
                _devId = value;
                _screenScraperService.DevId = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string DevPassword
        {
            get => _devPassword;
            set
            {
                _devPassword = value;
                _screenScraperService.DevPassword = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                _screenScraperService.Username = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                _screenScraperService.Password = value;
                OnPropertyChanged();
            }
        }

        public string SearchStatus
        {
            get => _searchStatus;
            set
            {
                _searchStatus = value;
                OnPropertyChanged();
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public GameInfo CurrentGameInfo
        {
            get => _currentGameInfo;
            set
            {
                _currentGameInfo = value;
                OnPropertyChanged();
            }
        }

        public string SelectedImageUrl
        {
            get => _selectedImageUrl;
            set
            {
                _selectedImageUrl = value;
                OnPropertyChanged();
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand DownloadImageCommand { get; }
        public ICommand DownloadIcon0Command { get; }
        public ICommand SaveCredentialsCommand { get; }

        // This will be set by the parent SingleModel
        public Func<string> GetCurrentDiscPath { get; set; }
        public Action<byte[], string> SetGameArt { get; set; }
        public Action<byte[], string> SetIcon0 { get; set; }

        private bool CanSearch()
        {
            var discPath = GetCurrentDiscPath?.Invoke();
            return !IsSearching && 
                   !string.IsNullOrEmpty(DevId) && 
                   !string.IsNullOrEmpty(DevPassword) &&
                   !string.IsNullOrEmpty(discPath) &&
                   File.Exists(discPath);
        }

        private async Task SearchGameAsync()
        {
            var discPath = GetCurrentDiscPath?.Invoke();
            if (string.IsNullOrEmpty(discPath) || !File.Exists(discPath))
            {
                SearchStatus = "No disc selected or file not found";
                return;
            }

            IsSearching = true;
            SearchStatus = "Calculating file hashes...";

            try
            {
                var fileInfo = new FileInfo(discPath);
                var fileSize = fileInfo.Length;

                SearchStatus = "Calculating MD5...";
                var md5 = await Task.Run(() => ScreenScraperService.CalculateMD5(discPath));
                
                SearchStatus = "Calculating SHA1...";
                var sha1 = await Task.Run(() => ScreenScraperService.CalculateSHA1(discPath));
                
                SearchStatus = "Calculating CRC32...";
                var crc32 = await Task.Run(() => ScreenScraperService.CalculateCRC32(discPath));

                SearchStatus = "Searching ScreenScraper database...";
                var gameInfo = await _screenScraperService.GetGameInfoAsync(discPath, fileSize, crc32, md5, sha1);

                if (gameInfo != null)
                {
                    CurrentGameInfo = gameInfo;
                    SearchStatus = $"Found: {gameInfo.Name}";
                }
                else
                {
                    SearchStatus = "Game not found in ScreenScraper database";
                }
            }
            catch (Exception ex)
            {
                SearchStatus = $"Error: {ex.Message}";
                CurrentGameInfo = null;
            }
            finally
            {
                IsSearching = false;
            }
        }

        private async Task DownloadImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            try
            {
                SearchStatus = "Downloading image...";
                using var httpClient = new System.Net.Http.HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                
                // Determine the image type from URL or content
                var extension = Path.GetExtension(new Uri(imageUrl).LocalPath).ToLower();
                if (string.IsNullOrEmpty(extension))
                    extension = ".png"; // Default to PNG

                SetGameArt?.Invoke(imageBytes, extension);
                SearchStatus = "Image downloaded successfully";
            }
            catch (Exception ex)
            {
                SearchStatus = $"Failed to download image: {ex.Message}";
            }
        }

        private void LoadCredentialsFromSettings()
        {
            try
            {
                _settings = ScreenScraperSettings.Load();
                DevId = _settings.DevId;
                DevPassword = _settings.DevPassword;
                Username = _settings.Username;
                Password = _settings.Password;
                SearchStatus = "Credentials loaded from settings";
            }
            catch (Exception ex)
            {
                SearchStatus = $"Failed to load settings: {ex.Message}";
                _settings = new ScreenScraperSettings();
            }
        }

        public void SaveCredentialsToSettings()
        {
            try
            {
                if (_settings == null)
                    _settings = new ScreenScraperSettings();

                _settings.DevId = DevId;
                _settings.DevPassword = DevPassword;
                _settings.Username = Username;
                _settings.Password = Password;
                _settings.Save();
                SearchStatus = "Credentials saved successfully";
            }
            catch (Exception ex)
            {
                SearchStatus = $"Failed to save settings: {ex.Message}";
            }
        }

        private async Task DownloadIcon0Async()
        {
            var iconUrl = CurrentGameInfo?.Media?.Icon0Url;
            if (string.IsNullOrEmpty(iconUrl))
                return;

            try
            {
                SearchStatus = "Downloading ICON0...";
                using var httpClient = new System.Net.Http.HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(iconUrl);
                
                // Save to artwork cache directory
                var artworkDir = ScreenScraperSettings.GetArtworkCacheDirectory();
                var fileName = $"{CurrentGameInfo.Id}_icon0.png";
                var filePath = Path.Combine(artworkDir, fileName);
                await File.WriteAllBytesAsync(filePath, imageBytes);
                
                // Set as ICON0
                SetIcon0?.Invoke(imageBytes, ".png");
                SearchStatus = $"ICON0 downloaded and cached: {fileName}";
            }
            catch (Exception ex)
            {
                SearchStatus = $"Failed to download ICON0: {ex.Message}";
            }
        }
    }
}