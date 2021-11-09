using System;
using System.Security.Cryptography;
using System.Text;
using SharpLab.Server.Common;

namespace SharpLab.Server.Caching.Internal {
    public class ResultCacheBuilder : IResultCacheBuilder {
        private readonly string? _branchId;
        private readonly MemoryPoolSlim<byte> _byteMemoryPool;
        private readonly byte[] _branchIdBytes;

        public ResultCacheBuilder(string? branchId, MemoryPoolSlim<byte> byteMemoryPool) {
            _branchId = branchId;
            _byteMemoryPool = byteMemoryPool;
            _branchIdBytes = branchId != null ? Encoding.UTF8.GetBytes(branchId) : Array.Empty<byte>();
        }

        public ResultCacheDetails Build(in ResultCacheKeyData key, ReadOnlyMemory<byte> resultBytes) {
            var secretKeyBytes = (stackalloc byte[32]);
            BuildSecretKey(key, secretKeyBytes);

            var iv = default(MemoryLease<byte>);
            var tag = default(MemoryLease<byte>);
            var encryptedData = default(MemoryLease<byte>);
            try {
                iv = _byteMemoryPool.RentExact(12);
                RandomNumberGenerator.Fill(iv.AsSpan());

                tag = _byteMemoryPool.RentExact(16);

                encryptedData = _byteMemoryPool.RentExact(resultBytes.Length);
                using (var aes = new AesGcm(secretKeyBytes))
                    aes.Encrypt(iv.AsSpan(), resultBytes.Span, encryptedData.AsSpan(), tag.AsSpan());

                var publicHashBytes = (stackalloc byte[32]);
                if (!SHA256.TryHashData(secretKeyBytes, publicHashBytes, out _))
                    throw new("SHA256 failed to hash secret key into public hash.");

                var publicHash = Convert.ToHexString(publicHashBytes).ToLowerInvariant();
                return new(
                    cacheKey: (_branchId ?? "default") + "/" + publicHash,
                    iv: iv,
                    tag: tag,
                    encryptedData: encryptedData
                );
            }
            catch {
                iv.Dispose();
                tag.Dispose();
                encryptedData.Dispose();
                throw;
            }
        }

        private void BuildSecretKey(in ResultCacheKeyData key, Span<byte> secretKey32Bytes) {
            var keyDataBytesLength = key.LanguageName.Length
                + 1 +
                key.TargetName.Length
                + 1 +
                key.Optimize.Length
                + 1 +
                _branchIdBytes.Length
                + 1 +
                Encoding.UTF8.GetBytes(key.Code).Length;

            using var keyBytesLease = _byteMemoryPool.RentExact(keyDataBytesLength);
            var keyBytesSpan = keyBytesLease.AsSpan();

            var position = 0;
            WriteOptionWithSeparator(keyBytesSpan, key.LanguageName, ref position);
            WriteOptionWithSeparator(keyBytesSpan, key.TargetName, ref position);
            WriteOptionWithSeparator(keyBytesSpan, key.Optimize, ref position);

            _branchIdBytes.CopyTo(keyBytesSpan.Slice(position));
            position += _branchIdBytes.Length;
            WriteSeparator(keyBytesSpan, ref position);

            position += Encoding.UTF8.GetBytes(key.Code, keyBytesSpan.Slice(position));

            if (!SHA256.TryHashData(keyBytesSpan, secretKey32Bytes, out _))
                throw new("SHA256 failed to hash cache key into secret key.");
        }

        private void WriteOptionWithSeparator(Span<byte> keyBytesSpan, string? value, ref int position) {
            if (value != null)
                position += Encoding.UTF8.GetBytes(value, keyBytesSpan.Slice(position));
            WriteSeparator(keyBytesSpan, ref position);
        }

        private void WriteSeparator(Span<byte> keyBytesSpan, ref int position) {
            keyBytesSpan[position] = (byte)'|';
            position += 1;
        }
    }
}
