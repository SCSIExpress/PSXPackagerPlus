using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PSXPackager.Common.ScreenScraper;

namespace PSXPackager.Tests
{
    [TestClass]
    public class ScreenScraperServiceTests
    {
        [TestMethod]
        public void CalculateMD5_ValidFile_ReturnsCorrectHash()
        {
            // Arrange
            var testContent = "Hello, World!";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, testContent);

            try
            {
                // Act
                var md5Hash = ScreenScraperService.CalculateMD5(tempFile);

                // Assert
                Assert.IsNotNull(md5Hash);
                Assert.AreEqual(32, md5Hash.Length); // MD5 hash should be 32 characters
                Assert.AreEqual("65a8e27d8879283831b664bd8b7f0ad4", md5Hash); // Known MD5 for "Hello, World!"
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void CalculateSHA1_ValidFile_ReturnsCorrectHash()
        {
            // Arrange
            var testContent = "Hello, World!";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, testContent);

            try
            {
                // Act
                var sha1Hash = ScreenScraperService.CalculateSHA1(tempFile);

                // Assert
                Assert.IsNotNull(sha1Hash);
                Assert.AreEqual(40, sha1Hash.Length); // SHA1 hash should be 40 characters
                Assert.AreEqual("0a0a9f2a6772942557ab5355d76af442f8f65e01", sha1Hash); // Known SHA1 for "Hello, World!"
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void CalculateCRC32_ValidFile_ReturnsCorrectHash()
        {
            // Arrange
            var testContent = "Hello, World!";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, testContent);

            try
            {
                // Act
                var crc32Hash = ScreenScraperService.CalculateCRC32(tempFile);

                // Assert
                Assert.IsNotNull(crc32Hash);
                Assert.AreEqual(8, crc32Hash.Length); // CRC32 hash should be 8 characters
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void ScreenScraperService_Constructor_InitializesCorrectly()
        {
            // Act
            var service = new ScreenScraperService();

            // Assert
            Assert.IsNotNull(service);
            Assert.IsNull(service.DevId);
            Assert.IsNull(service.DevPassword);
            Assert.AreEqual("PSXPackagerPlus", service.SoftName);
        }

        [TestMethod]
        public async Task GetGameInfoAsync_WithoutCredentials_ThrowsException()
        {
            // Arrange
            var service = new ScreenScraperService();
            var tempFile = Path.GetTempFileName();

            try
            {
                // Act & Assert
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    () => service.GetGameInfoAsync(tempFile, 1024));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void GameInfo_Constructor_InitializesCorrectly()
        {
            // Act
            var gameInfo = new GameInfo();

            // Assert
            Assert.IsNotNull(gameInfo);
            Assert.IsNotNull(gameInfo.Genres);
            Assert.IsNotNull(gameInfo.Media);
            Assert.AreEqual(0, gameInfo.Genres.Count);
        }

        [TestMethod]
        public void MediaInfo_Constructor_InitializesCorrectly()
        {
            // Act
            var mediaInfo = new MediaInfo();

            // Assert
            Assert.IsNotNull(mediaInfo);
            Assert.IsNull(mediaInfo.Screenshot);
            Assert.IsNull(mediaInfo.Fanart);
            Assert.IsNull(mediaInfo.Video);
        }
    }
}