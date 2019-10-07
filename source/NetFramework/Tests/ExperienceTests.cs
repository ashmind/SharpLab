using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.QuickInfo;
using MirrorSharp.Testing;
using MirrorSharp.Testing.Internal;
using SharpLab.Tests.Internal;
using Xunit;

namespace SharpLab.Tests {
    public class ExperienceTests {
        [Fact]
        public async Task RequestInfoTip_IncludesXmlDocumentation() {
            var textWithCursor = TextWithCursor.Parse("class C { string M(int a) { return a.To➭String(); } }", '➭');
            var driver = MirrorSharpTestDriver.New(TestEnvironment.MirrorSharpOptions)
                .SetText(textWithCursor.Text);
            var result = await driver.SendRequestInfoTipAsync(textWithCursor.CursorPosition);

            var documentation = Assert.Single(result.Sections.Where(e => e.Kind == QuickInfoSectionKinds.DocumentationComments.ToLowerInvariant()));
            Assert.Equal(
                "Converts the numeric value of this instance to its equivalent string representation.",
                documentation.ToString()
            );
        }
    }
}