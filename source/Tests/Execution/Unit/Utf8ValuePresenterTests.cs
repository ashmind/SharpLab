using System;
using System.Linq;
using System.Text;
using SharpLab.Container.Runtime;
using SharpLab.Runtime.Internal;
using Xunit;

namespace SharpLab.Tests.Execution.Unit {
    public class Utf8ValuePresenterTests {
        [Theory]
        [InlineData("1234", "1234")]
        [InlineData("12345", "12345")]
        [InlineData("123456", "1234…")]
        [InlineData("123💔56", "123�…")]
        public void Present_TruncatesStringCorrectly(string value, string expected) {
            Assert.Equal(expected, Present(value, new (maxValueLength: 5)));
        }

        [Theory]
        [InlineData(new[] { "1234" }, "{ 1234 }")]
        [InlineData(new[] { "1", "2", "3" }, "{ 1, 2, 3 }")]
        [InlineData(new[] { "1", "2", "3", "4" }, "{ 1, 2, 3, … }")]
        [InlineData(new[] { "123456", "123456", "123456",  }, "{ 1234…, 1234…, 1234… }")]
        public void Present_TruncatesListCorrectly(string[] values, string expected) {
            Assert.Equal(expected, Present(values, new(
                maxValueLength: 5,
                maxEnumerableItemCount: 3
            )));
        }

        [Theory]
        [MemberData(nameof(DepthTheoryData))]
        public void Present_LimitsDepthCorrectly(object[] values, string expected) {
            Assert.Equal(expected, Present(values, new(maxValueLength: 5)));
        }

        [Fact]
        public void GetMaxOutputByteCount_MatchesActualMaximumLength() {
            var limits = new ValuePresenterLimits(
                maxValueLength: 10,
                maxEnumerableItemCount: 3
            );
            var presented = Present(
                Enumerable.Range(0, 10)
                    .Select(_ => Enumerable.Range(0, 10).Select(_ => "1234567890+").ToList())
                    .ToList(),
                limits
            );

            var calculatedLength = new Utf8ValuePresenter()
                .GetMaxOutputByteCount(limits);

            Assert.Equal(Encoding.UTF8.GetByteCount(presented), calculatedLength);
        }

        public static TheoryData DepthTheoryData { get; } = new TheoryData<object[], string> {
            { new object[] { 1, 2, new[] { 1, 2, 3 } }, "{ 1, 2, { 1, … } }" },
            { new object[] { new object[] { new[] { 1 } } }, "{ { {…} } }" }
        };

        private string Present<T>(T value, ValuePresenterLimits limits) {
            var bytes = (Span<byte>)stackalloc byte[256];
            new Utf8ValuePresenter()
                .Present(bytes, VariantValue.From(value), limits, out var byteCount);
            return Encoding.UTF8.GetString(bytes.Slice(0, byteCount));
        }
    }
}
