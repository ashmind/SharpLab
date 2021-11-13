using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Mocks;
using SourceMock.Internal;
using Xunit;
using SharpLab.Server.Integration.Azure;
using SharpLab.Server.Monitoring.Mocks;

namespace SharpLab.Tests.Caching.Unit {
    public class AzureBlobResultCacheStoreTests {
        [Fact]
        public async Task StoreAsync_DoesNotCallUploadBlobAsync_ForSecondCallWithSameKey() {
            // Arrange
            var blobContainerMock = new BlobContainerClientMock();
            var store = new AzureBlobResultCacheStore(blobContainerMock, "_", new MonitorMock());
            await store.StoreAsync("test-key", new MemoryStream(), CancellationToken.None);

            // Act
            await store.StoreAsync("test-key", new MemoryStream(), CancellationToken.None);

            // Assert
            Assert.Equal(1, blobContainerMock.Calls.UploadBlobAsync(content: default(MockArgumentMatcher<Stream>)).Count);
        }
    }
}
