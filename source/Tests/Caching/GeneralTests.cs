using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Autofac;
using SharpLab.Server.Caching.Internal.Mocks;
using SharpLab.Server.Common;
using SharpLab.Tests.Internal;
using Xunit;

namespace SharpLab.Tests.Caching {
    public class GeneralTests {
        [Fact]
        public async Task SlowUpdate_AddsResultToCache_WhenRunForFirstTime() {
            // Arrange
            var cache = TestEnvironment.Container.Resolve<ResultCacheStoreMock>();
            var driver = TestEnvironment.NewDriver();
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.IL);
            var cacheKey = (string?)null;
            var cacheBytes = (byte[]?)null;
            cache.Setup.StoreAsync().Runs((key, stream, _) => {
                cacheKey = key;
                cacheBytes = ((MemoryStream)stream).ToArray();
                return Task.CompletedTask;
            });

            // Act
            await driver.SendSlowUpdateAsync();

            // Assert
            Assert.Equal("sl-test/58b7e1a46ace8243ab08bc6e6014cdbd8d1d30c705b3e963293d6a1e1ff1b99a", cacheKey);
            var json = Encoding.UTF8.GetString(cacheBytes!);
            var cached = JsonSerializer.Deserialize<CacheData>(json, new() {
                PropertyNameCaseInsensitive = true
            })!;
            Assert.Equal(1, cached.Version);
            Assert.NotNull(cached.Date);
            Assert.NotNull(cached.Encrypted.Tag);
            Assert.NotNull(cached.Encrypted.IV);
            Assert.NotNull(cached.Encrypted.Data);
        }

        [Fact]
        public async Task SlowUpdate_DoesNotAddResultToCache_WhenRunForSecondTime() {
            // Arrange
            var cache = TestEnvironment.Container.Resolve<ResultCacheStoreMock>();
            var driver = TestEnvironment.NewDriver();
            await driver.SendSetOptionsAsync(LanguageNames.CSharp, TargetNames.IL);

            // Act
            await driver.SendSlowUpdateAsync();
            await driver.SendSlowUpdateAsync();

            // Assert
            Assert.Equal(1, cache.Calls.StoreAsync().Count);
        }

        private record CacheData(int Version, string Date, CacheDataEncrypted Encrypted);
        private record CacheDataEncrypted(string Tag, string IV, string Data);
    }
}
