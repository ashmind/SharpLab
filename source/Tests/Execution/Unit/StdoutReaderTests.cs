using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Mocks;
using SharpLab.Container.Manager.Internal;
using Xunit;

namespace SharpLab.Tests.Execution.Unit {
    public class StdoutReaderTests {
        [Theory]
        [InlineData(new[] { "START", "ABC", "END" }, "ABC")]
        [InlineData(new[] { "ST", "ARTA", "B", "CEN", "D" }, "ABC")]
        [InlineData(new[] { "STARTA", "BC", "END" }, "ABC")]
        [InlineData(new[] { "START", "AB", "CEND" }, "ABC")]
        [InlineData(new[] { "STARTABCEND" }, "ABC")]
        [InlineData(new[] { "STARTEND" }, "")]
        [InlineData(new[] { "STAR", "TEND" }, "")]
        public async Task ReadOutputAsync_DetectsOutputBoundariesCorrectly(string[] segments, string expectedOutput) {
            var reader = new StdoutReader(new LoggerMock<StdoutReader>());
            var inputStream = new Utf8SegmentedAsyncStream(segments);

            var outputBuffer = new byte[10240];
            using var cancellationTokenSource = new CancellationTokenSource(
                Debugger.IsAttached ? TimeSpan.FromMinutes(15) : TimeSpan.FromSeconds(30)
            );
            var result = await reader.ReadOutputAsync(
                inputStream,
                outputBuffer,
                Encoding.UTF8.GetBytes("START"),
                Encoding.UTF8.GetBytes("END"),
                cancellationTokenSource.Token
            );
                        
            Assert.Equal(expectedOutput, Encoding.UTF8.GetString(result.Output.Span));
            Assert.True(result.IsOutputReadSuccess);
        }

        private class Utf8SegmentedAsyncStream : Stream {
            private readonly IReadOnlyList<string> _segments;
            private int _segmentIndex = -1;

            public Utf8SegmentedAsyncStream(IReadOnlyList<string> segments) {
                _segments = segments;
            }

            public override bool CanRead => throw new NotImplementedException();
            public override bool CanSeek => throw new NotImplementedException();
            public override bool CanWrite => throw new NotImplementedException();
            public override long Length => throw new NotImplementedException();
            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override void Flush() => throw new NotImplementedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
                _segmentIndex += 1;
                if (_segmentIndex >= _segments.Count)
                    return Task.FromResult(0);

                return Task.FromResult(
                    Encoding.UTF8.GetBytes(_segments[_segmentIndex].AsSpan(), buffer.AsSpan(offset, count))
                );
            }
        }
    }
}
