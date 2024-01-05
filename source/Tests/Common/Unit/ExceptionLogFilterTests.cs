using MirrorSharp.Advanced.Mocks;
using SharpLab.Server.Common;
using System;
using Xunit;

namespace SharpLab.Tests.Common.Unit; 
public class ExceptionLogFilterTests {
    [Fact]
    public void ShouldLog_ReturnsTrue_ForGeneralException() {
        // Arrange
        var filter = new ExceptionLogFilter();

        // Act
        var shouldLog = filter.ShouldLog(new Exception(), new WorkSessionMock());

        // Assert
        Assert.True(shouldLog);
    }

    [Fact]
    public void ShouldLog_ReturnsFalse_ForBadImageFormatException_WithILEmitByte() {
        // Arrange
        var filter = new ExceptionLogFilter();
        var session = new WorkSessionMock();
        session.Setup.LanguageName.Returns(LanguageNames.IL);
        session.Setup.GetText().Returns("ABC .emitbyte DEF");

        // Act
        var shouldLog = filter.ShouldLog(new BadImageFormatException(), session);

        // Assert
        Assert.False(shouldLog);
    }
}
