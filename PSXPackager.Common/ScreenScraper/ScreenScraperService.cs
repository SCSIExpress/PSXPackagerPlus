using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PSXPackager.Common.ScreenScraper
{
    public class ScreenScraperService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.screenscraper.fr/api2";
        
        public string DevId { get; set; }
        public string DevPassword { get; set; }
        public string SoftName { get; set; } = "PSXPackagerPlus";
        public string Username { get; set; }
        public string Password { get; set; }

        public ScreenScraperService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<GameInfo> GetGameInfoAsync(string filePath, long fileSize, string crc32 = null, string md5 = null, string sha1 = null)
        {
            if (string.IsNullOrEmpty(DevId) || string.IsNullOrEmpty(DevPassword))
                throw new InvalidOperationException("Developer credentials are required");

            var parameters = new Dictionary<string, string>
            {
                ["devid"] = DevId,
                ["devpassword"] = DevPassword,
                ["softname"] = SoftName,
                ["output"] = "xml",
                ["systemeid"] = "57", // PlayStation system ID
                ["romtype"] = "iso",
                ["romnom"] = Path.GetFileName(filePath),
                ["romsize"] = fileSize.ToString()
            };

            if (!string.IsNullOrEmpty(Username))
                parameters["ssid"] = Username;
            
            if (!string.IsNullOrEmpty(Password))
                parameters["sspassword"] = Password;

            if (!string.IsNullOrEmpty(crc32))
                parameters["crc"] = crc32;
            
            if (!string.IsNullOrEmpty(md5))
                parameters["md5"] = md5;
            
            if (!string.IsNullOrEmpty(sha1))
                parameters["sha1"] = sha1;

            var url = BuildUrl("jeuInfos.php", parameters);
            
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                return ParseGameInfo(response);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Failed to retrieve game information: {ex.Message}", ex);
            }
        }

        public static string CalculateMD5(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static string CalculateSHA1(string filePath)
        {
            using var sha1 = SHA1.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha1.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static string CalculateCRC32(string filePath)
        {
            // Simple CRC32 implementation - you might want to use a more robust library
            const uint polynomial = 0xEDB88320;
            var table = new uint[256];
            
            for (uint i = 0; i < 256; i++)
            {
                var crc = i;
                for (var j = 8; j > 0; j--)
                {
                    if ((crc & 1) == 1)
                        crc = (crc >> 1) ^ polynomial;
                    else
                        crc >>= 1;
                }
                table[i] = crc;
            }

            using var stream = File.OpenRead(filePath);
            var buffer = new byte[4096];
            uint crc32 = 0xFFFFFFFF;
            
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (var i = 0; i < bytesRead; i++)
                {
                    var index = (crc32 ^ buffer[i]) & 0xFF;
                    crc32 = (crc32 >> 8) ^ table[index];
                }
            }
            
            return (~crc32).ToString("x8");
        }

        private string BuildUrl(string endpoint, Dictionary<string, string> parameters)
        {
            var url = new StringBuilder($"{BaseUrl}/{endpoint}?");
            
            foreach (var param in parameters)
            {
                url.Append($"{param.Key}={Uri.EscapeDataString(param.Value)}&");
            }
            
            return url.ToString().TrimEnd('&');
        }

        private GameInfo ParseGameInfo(string xmlResponse)
        {
            try
            {
                var doc = XDocument.Parse(xmlResponse);
                var gameElement = doc.Root?.Element("jeu");
                
                if (gameElement == null)
                    return null;

                var gameInfo = new GameInfo
                {
                    Id = gameElement.Attribute("id")?.Value,
                    Name = GetGameName(gameElement.Element("noms")),
                    Synopsis = GetSynopsis(gameElement.Element("synopsis")),
                    Publisher = gameElement.Element("editeur")?.Value,
                    Developer = gameElement.Element("developpeur")?.Value,
                    Players = gameElement.Element("joueurs")?.Value,
                    Rating = gameElement.Element("note")?.Value,
                    ReleaseDate = GetReleaseDate(gameElement.Element("dates")),
                    Genres = GetGenres(gameElement.Element("genres")),
                    Media = ParseMedia(gameElement.Element("medias"))
                };

                return gameInfo;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse game information: {ex.Message}", ex);
            }
        }

        private string GetGameName(XElement nomsElement)
        {
            if (nomsElement == null) return null;
            
            // Try to get US name first, then JP, then any available
            var usName = nomsElement.Elements("nom").FirstOrDefault(e => e.Attribute("region")?.Value == "us");
            var jpName = nomsElement.Elements("nom").FirstOrDefault(e => e.Attribute("region")?.Value == "jp");
            var anyName = nomsElement.Elements("nom").FirstOrDefault();
            
            return usName?.Value ?? jpName?.Value ?? anyName?.Value;
        }

        private string GetSynopsis(XElement synopsisElement)
        {
            if (synopsisElement == null) return null;
            
            // Try to get English synopsis first
            var enSynopsis = synopsisElement.Elements("synopsis").FirstOrDefault(e => e.Attribute("langue")?.Value == "en");
            var anySynopsis = synopsisElement.Elements("synopsis").FirstOrDefault();
            
            return enSynopsis?.Value ?? anySynopsis?.Value;
        }

        private string GetReleaseDate(XElement datesElement)
        {
            if (datesElement == null) return null;
            
            // Try to get US date first, then JP, then any available
            var usDate = datesElement.Elements("date").FirstOrDefault(e => e.Attribute("region")?.Value == "us");
            var jpDate = datesElement.Elements("date").FirstOrDefault(e => e.Attribute("region")?.Value == "jp");
            var anyDate = datesElement.Elements("date").FirstOrDefault();
            
            return usDate?.Value ?? jpDate?.Value ?? anyDate?.Value;
        }

        private List<string> GetGenres(XElement genresElement)
        {
            var genres = new List<string>();
            if (genresElement == null) return genres;

            // Get English genre names
            foreach (var genre in genresElement.Elements("genre"))
            {
                var langue = genre.Attribute("langue")?.Value;
                if (langue == "en" || string.IsNullOrEmpty(langue))
                {
                    genres.Add(genre.Value);
                }
            }

            return genres;
        }

        private MediaInfo ParseMedia(XElement mediasElement)
        {
            if (mediasElement == null) return new MediaInfo();

            var mediaInfo = new MediaInfo();

            // Parse all media elements
            foreach (var mediaElement in mediasElement.Elements("media"))
            {
                var type = mediaElement.Attribute("type")?.Value;
                var region = mediaElement.Attribute("region")?.Value;
                var url = mediaElement.Value;

                switch (type)
                {
                    case "ss":
                        mediaInfo.Screenshot = url;
                        break;
                    case "fanart":
                        mediaInfo.Fanart = url;
                        break;
                    case "video":
                        mediaInfo.Video = url;
                        break;
                    case "screenmarquee":
                        mediaInfo.Marquee = url;
                        break;
                    case "wheel-hd":
                        if (region == "us") mediaInfo.WheelUs = url;
                        else if (region == "jp") mediaInfo.WheelJp = url;
                        break;
                }
            }

            // Look for box-2D media with region attributes for ICON0
            var box2DElements = mediasElement.Elements("media").Where(e => 
                e.Attribute("type")?.Value == "box-2D");

            var usBox2D = box2DElements.FirstOrDefault(e => e.Attribute("region")?.Value == "us");
            var jpBox2D = box2DElements.FirstOrDefault(e => e.Attribute("region")?.Value == "jp");

            mediaInfo.Icon0Url = usBox2D?.Value ?? jpBox2D?.Value;

            return mediaInfo;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}