using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Threading;
using Popstation.Database;
using PSXPackager.Common;
using PSXPackager.Common.Notification;
using PSXPackager.Common.ScreenScraper;
using PSXPackagerGUI.Models;
using PSXPackagerGUI.Pages;

namespace PSXPackagerGUI.Processing
{
    public class Processor
    {
        private readonly Dispatcher _dispatcher;
        private readonly GameDB _gameDb;
        private readonly SettingsModel _settings;
        private readonly IEventHandler _eventHandler;
        private readonly Channel<ConvertJob> _channel = Channel.CreateUnbounded<ConvertJob>();
        private int _degreeOfParallelism;

        public Processor(Dispatcher dispatcher, GameDB gameDb, SettingsModel settings, IEventHandler eventHandler)
        {
            _degreeOfParallelism = 4;
            _dispatcher = dispatcher;
            _gameDb = gameDb;
            _settings = settings;
            _eventHandler = eventHandler;
        }

        public void Add(ConvertJob job)
        {
            _channel.Writer.WriteAsync(job);
        }

        public Task Start(BatchModel model, CancellationToken token)
        {
            var consumers = new List<Task>();

            for (var i = 0; i < _degreeOfParallelism; i++)
            {
                consumers.Add(ProcessTask(model, token));
            }

            _channel.Writer.Complete();

            return Task.WhenAll(consumers);
        }

        private async Task ProcessTask(BatchModel model, CancellationToken token)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "PSXPackager");

            
            while (await _channel.Reader.WaitToReadAsync())
            {
                var job = await _channel.Reader.ReadAsync();

                var notifier = new ProcessNotifier(_dispatcher);
                notifier.Entry = job.Entry;

                var processing = new Popstation.Processing(notifier, _eventHandler, _gameDb);

                // ScreenScraper integration
                string screenScraperIcon0Path = null;
                if (model.UseScreenScraper)
                {
                    screenScraperIcon0Path = await DownloadScreenScraperArtworkAsync(Path.Combine(model.Settings.InputPath, job.Entry.RelativePath), job.Entry);
                }

                var processOptions = new ProcessOptions()
                {
                    ////Files = files,
                    OutputPath = model.Settings.OutputPath,
                    TempPath = tempPath,
                    Discs = Enumerable.Range(1, 5).ToList(),
                    //CheckIfFileExists = !o.OverwriteIfExists,
                    //SkipIfFileExists = o.SkipIfExists,
                    FileNameFormat = _settings.FileNameFormat,
                    CompressionLevel = _settings.CompressionLevel,
                    ////Verbosity = o.Verbosity,
                    ////Log = o.Log,
                    ExtractResources = model.ExtractResources,
                    ImportResources = _settings.UseCustomResources || !string.IsNullOrEmpty(screenScraperIcon0Path),
                    GenerateResourceFolders = model.GenerateResourceFolders,
                    ResourceFormat = _settings.CustomResourcesFormat,
                    ResourceRoot = !string.IsNullOrEmpty(screenScraperIcon0Path) ? 
                        ScreenScraperSettings.GetArtworkCacheDirectory() : _settings.CustomResourcesPath, 
                };

                // If we downloaded ScreenScraper artwork, copy it to the expected location
                if (!string.IsNullOrEmpty(screenScraperIcon0Path))
                {
                    await SetupScreenScraperResourceAsync(screenScraperIcon0Path, job.Entry.RelativePath, processOptions);
                }

                await Task.Run(() =>
                {
                    processing.ProcessFile(Path.Combine(model.Settings.InputPath, job.Entry.RelativePath), processOptions, token);
                });

            }
        }

        private Task SetupScreenScraperResourceAsync(string artworkPath, string relativePath, ProcessOptions processOptions)
        {
            try
            {
                // Create a simple resource structure in the artwork cache directory
                // We'll use the filename as the resource folder name
                var fileName = Path.GetFileNameWithoutExtension(relativePath);
                var resourceDir = Path.Combine(ScreenScraperSettings.GetArtworkCacheDirectory(), fileName);
                Directory.CreateDirectory(resourceDir);

                // Copy the artwork as ICON0.png
                var icon0Path = Path.Combine(resourceDir, "ICON0.png");
                File.Copy(artworkPath, icon0Path, true);

                // Update the resource format to use this structure
                processOptions.ResourceFormat = "%FILENAME%\\%RESOURCE%.%EXT%";
            }
            catch (System.Exception)
            {
                // If setup fails, just continue without custom resources
            }

            return Task.CompletedTask;
        }

        private async Task<string> DownloadScreenScraperArtworkAsync(string filePath, BatchEntryModel entry)
        {
            try
            {
                // Update entry status
                _dispatcher.Invoke(() => entry.Status = "Downloading artwork...");

                // Load ScreenScraper settings
                var settings = ScreenScraperSettings.Load();
                if (string.IsNullOrEmpty(settings.DevId) || string.IsNullOrEmpty(settings.DevPassword))
                {
                    _dispatcher.Invoke(() => entry.Status = "ScreenScraper credentials not configured");
                    return null;
                }

                // Initialize ScreenScraper service
                var screenScraperService = new ScreenScraperService
                {
                    DevId = settings.DevId,
                    DevPassword = settings.DevPassword,
                    Username = settings.Username,
                    Password = settings.Password
                };

                // Calculate file hashes
                var fileInfo = new FileInfo(filePath);
                var md5 = await Task.Run(() => ScreenScraperService.CalculateMD5(filePath));
                var sha1 = await Task.Run(() => ScreenScraperService.CalculateSHA1(filePath));
                var crc32 = await Task.Run(() => ScreenScraperService.CalculateCRC32(filePath));

                // Get game info from ScreenScraper
                var gameInfo = await screenScraperService.GetGameInfoAsync(filePath, fileInfo.Length, crc32, md5, sha1);

                if (gameInfo?.Media?.Icon0Url != null)
                {
                    // Download the ICON0 image
                    using var httpClient = new HttpClient();
                    var imageBytes = await httpClient.GetByteArrayAsync(gameInfo.Media.Icon0Url);

                    // Save to artwork cache directory
                    var artworkDir = ScreenScraperSettings.GetArtworkCacheDirectory();
                    var fileName = $"{gameInfo.Id}_icon0.png";
                    var artworkPath = Path.Combine(artworkDir, fileName);
                    await File.WriteAllBytesAsync(artworkPath, imageBytes);

                    _dispatcher.Invoke(() => entry.Status = $"Artwork downloaded: {fileName}");
                    return artworkPath;
                }
                else
                {
                    _dispatcher.Invoke(() => entry.Status = "No artwork found on ScreenScraper");
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                _dispatcher.Invoke(() => entry.Status = $"ScreenScraper error: {ex.Message}");
                return null;
            }
        }

    }
}