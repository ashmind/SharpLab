using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AshMind.Extensions;
using Autofac;
using Microsoft.CodeAnalysis;
using TryRoslyn.Core;
using TryRoslyn.Core.Modules;
using TryRoslyn.Core.Processing;
using Xunit;
using Xunit.Extensions;

namespace TryRoslyn.Tests {
    public class LocalCodeProcessorTests {
        private static readonly IDictionary<string, LanguageIdentifier> LanguageMap = new Dictionary<string, LanguageIdentifier> {
            { "cs", LanguageIdentifier.CSharp },
            { "vb", LanguageIdentifier.VBNet },
            { "il", LanguageIdentifier.IL }
        };
            
        [Theory]
        [InlineData("Constructor.BaseCall.cs2cs")]
        [InlineData("Constructor.ArgumentAssignedToField.cs2cs")]
        [InlineData("NullPropagation.ToTernary.cs2cs")]
        [InlineData("Script.cs2cs")]
        [InlineData("Simple.cs2il")]
        [InlineData("Simple.vb2vb")]
        [InlineData("Module.vb2vb")]
        public void Process_ReturnsExpectedCode(string resourceName) {
            var content = GetResourceContent(resourceName);
            var parts = content.Split("?=>");
            var code = parts[0].Trim();
            var expected = parts[1].Trim();

            var service = CreateService();
            var result = service.Process(code, GetProcessingOptions(resourceName, content));

            var errors = string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            Assert.Equal("", errors);
            AssertGold.Equal(expected, result.Decompiled.Trim());
        }

        private string GetResourceContent(string name) {
            var fullName = GetType().Namespace + ".TestCode." + name;
            // ReSharper disable once AssignNullToNotNullAttribute
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName)) {
                if (stream == null)
                    throw new FileNotFoundException("Resource was not found.", fullName);

                using (var reader = new StreamReader(stream)) {
                    return reader.ReadToEnd();
                }
            }
        }

        private ProcessingOptions GetProcessingOptions(string resourceName, string content) {
            var scriptMode = content.StartsWith("// script mode") || content.StartsWith("' Script Mode");
            var fromTo = Path.GetExtension(resourceName).TrimStart('.').Split('2');

            return new ProcessingOptions {
                SourceLanguage = LanguageMap[fromTo[0]],
                TargetLanguage = LanguageMap[fromTo[1]],
                ScriptMode = scriptMode
            };
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
