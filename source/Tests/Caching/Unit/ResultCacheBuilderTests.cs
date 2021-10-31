using System;
using System.Text;
using SharpLab.Server.Caching;
using SharpLab.Server.Caching.Internal;
using SharpLab.Server.Common;
using Xunit;

namespace SharpLab.Tests.Caching.Unit {
    public class ResultCacheBuilderTests {
        [Theory]
        [InlineData("branch", LanguageNames.CSharp, TargetNames.CSharp, Optimize.Debug, "test code", "branch/5aff343407aa8328fa32c34fec412261171c62d33de0dae11ab48f6c5082954b")]
        [InlineData("branch", LanguageNames.CSharp, TargetNames.CSharp, Optimize.Debug, "test code 2", "branch/67b3ef62ced8cd27170598066f9ec66dfe455312dcf5a6876d492c3943e8ffb6")]
        [InlineData("branch", LanguageNames.CSharp, TargetNames.CSharp, Optimize.Release, "test code", "branch/f0bd5e67096cd42fd590eaf4e19de8413f2dade8f5aca39a8c572b789d4f637c")]
        [InlineData("branch", LanguageNames.CSharp, TargetNames.IL, Optimize.Debug, "test code", "branch/6a51e5e5f51c0dd69a945db26629e5827a14995c81dcfbcad99d690d0f3329d2")]
        [InlineData("branch", LanguageNames.IL, TargetNames.CSharp, Optimize.Debug, "test code", "branch/3ef7679726bbb5f63ae69838b0e3188c927974b1a4eeda8a3f25407976c2d28c")]
        [InlineData(null, LanguageNames.CSharp, TargetNames.CSharp, Optimize.Debug, "test code", "default/78ec17551e4d752128f283d01e58c7c5e9fd921c7cb5fb27696c05012097e7a5")]
        public void Build_ReturnsExpectedCacheKey(
            string? branchId, string languageName, string targetName, string optimize, string code,
            string expectedCacheKey
        ) {
            // Arrange
            var builder = new ResultCacheBuilder(branchId, MemoryPoolSlim<byte>.Shared);

            // Act
            using var result = builder.Build(new(
                languageName,
                targetName,
                optimize,
                code
            ), Array.Empty<byte>());

            // Assert
            Assert.Equal(expectedCacheKey, result.CacheKey);
        }

        [Fact]
        public void Build_ReturnsUniqueIV_OnEachCall() {
            // Arrange
            var key = new ResultCacheKeyData(LanguageNames.CSharp, TargetNames.CSharp, Optimize.Debug, "test");
            var builder = new ResultCacheBuilder(null, MemoryPoolSlim<byte>.Shared);

            // Act
            using var result1 = builder.Build(key, Array.Empty<byte>());
            using var result2 = builder.Build(key, Array.Empty<byte>());

            // Assert
            Assert.NotEqual(
                Convert.ToBase64String(result1.IV.AsSpan()),
                Convert.ToBase64String(result2.IV.AsSpan())
            );
        }

        [Fact]
        public void Build_ReturnsUniqueEncryptedData_OnEachCall() {
            // Arrange
            var key = new ResultCacheKeyData(LanguageNames.CSharp, TargetNames.CSharp, Optimize.Debug, "test");
            var resultBytes = Encoding.UTF8.GetBytes("result");
            var builder = new ResultCacheBuilder(null, MemoryPoolSlim<byte>.Shared);

            // Act
            using var result1 = builder.Build(key, resultBytes);
            using var result2 = builder.Build(key, resultBytes);

            // Assert
            Assert.NotEqual(
                Convert.ToBase64String(result1.EncryptedData.AsSpan()),
                Convert.ToBase64String(result2.EncryptedData.AsSpan())
            );
        }
    }
}
