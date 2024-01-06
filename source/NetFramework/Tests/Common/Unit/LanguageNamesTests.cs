using Xunit;
using SharpLab.Server.Common;
using System.Linq;
using System.Reflection;

namespace SharpLab.Tests.Common.Unit;

public class LanguageNamesTests {
    [Fact]
    public void All_IncludesAllConstants() {
        // Arrange
        var constants = typeof(LanguageNames)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!);                

        // Act
        var all = LanguageNames.All;

        // Assert
        Assert.Equal(constants, all);
    }
}
