using System;
using System.Collections.Generic;
using System.Linq;
using AshMind.Extensions;
using Autofac;
using Microsoft.CodeAnalysis;
using TryRoslyn.Core;
using TryRoslyn.Core.Modules;
using TryRoslyn.Core.Processing;
using TryRoslyn.Tests.Support;
using Xunit;
using Xunit.Extensions;

namespace TryRoslyn.Tests {
    public class LocalCodeProcessorTests {
        [Theory]
        [EmbeddedResourceData("TestCode")]
        public void Process_ReturnsExpectedCode(string resourceName, string content) {
            var scriptMode = content.StartsWith("// script mode") || content.StartsWith("' Script Mode");
            var language = resourceName.EndsWith(".cstest") ? LanguageIdentifier.CSharp : LanguageIdentifier.VBNet;

            var parts = content.Split(new [] { "// =>", "' =>" });
            var code = parts[0].Trim();
            var expected = parts[1].Trim();

            var service = CreateService();
            var result = service.Process(code, new ProcessingOptions {
                SourceLanguage = language,
                TargetLanguage = language,
                ScriptMode = scriptMode
            });

            var errors = string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            Assert.Equal("", errors);
            AssertGold.Equal(expected, result.Decompiled.Trim());
        }

        private static LocalCodeProcessor CreateService() {
            var builder = new ContainerBuilder();
            builder.RegisterModule<LocalProcessingModule>();
            builder.RegisterType<LocalCodeProcessor>().AsSelf();
            var container = builder.Build();

            return container.Resolve<LocalCodeProcessor>();
        }
    }
}
