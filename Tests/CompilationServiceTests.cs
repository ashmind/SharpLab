using System;
using System.Collections.Generic;
using System.Linq;
using AshMind.Extensions;
using TryRoslyn.Core;
using TryRoslyn.Core.Decompilation;
using TryRoslyn.Tests.Support;
using Xunit;
using Xunit.Extensions;

namespace TryRoslyn.Tests {
    public class CompilationServiceTests {
        [Theory]
        [EmbeddedResourceData("TestCode")]
        public void EmbeddedResourceTests(string content) {
            var parts = content.Split("// =>");
            var code = parts[0].Trim();
            var expected = parts[1].Trim();

            var service = new CompilationService(new Decompiler());
            var result = service.Process(code);

            Assert.Equal(expected, result.Decompiled.Trim());
        }
    }
}
