using System;
using System.Collections.Generic;
using System.Linq;
using AshMind.Extensions;
using TryRoslyn.Core.Processing;
using TryRoslyn.Core.Processing.RoslynSupport;
using TryRoslyn.Tests.Support;
using Xunit;
using Xunit.Extensions;

namespace TryRoslyn.Tests {
    public class LocalCodeProcessorTests {
        [Theory]
        [EmbeddedResourceData("TestCode")]
        public void Process_ReturnsExpectedCode(string content) {
            var parts = content.Split("// =>");
            var code = parts[0].Trim();
            var expected = parts[1].Trim();

            var service = new LocalCodeProcessor(new Decompiler(), new RoslynAbstraction());
            var result = service.Process(code);

            Assert.Equal(expected, result.Decompiled.Trim());
        }
    }
}
