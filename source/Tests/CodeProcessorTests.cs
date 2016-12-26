//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using AshMind.Extensions;
//using Autofac;
//using TryRoslyn.Core;
//using TryRoslyn.Core.Processing;
//using Xunit;

//namespace TryRoslyn.Tests {
//    public class CodeProcessorTests {
//        private static readonly IDictionary<string, LanguageIdentifier> LanguageMap = new Dictionary<string, LanguageIdentifier> {
//            { "cs", LanguageIdentifier.CSharp },
//            { "vb", LanguageIdentifier.VBNet },
//            { "il", LanguageIdentifier.IL }
//        };

//        [Theory]
//        [InlineData("Constructor.BaseCall.cs2cs")]
//        [InlineData("NullPropagation.ToTernary.cs2cs")]
//        [InlineData("Script.cs2cs")]
//        [InlineData("Simple.cs2il")]
//        [InlineData("Simple.vb2vb")]
//        [InlineData("Module.vb2vb")]
//        public void Process_ReturnsExpectedCode(string resourceName) {
//            var content = GetResourceContent(resourceName);
//            var parts = content.Split("?=>");
//            var code = parts[0].Trim();
//            var expected = parts[1].Trim();

//            var result = CreateProcessor().Process(code, GetProcessingOptions(resourceName, content));

//            Assert.True(result.IsSuccess, GetErrorString(result));
//            AssertGold.Equal(expected, result.Decompiled.Trim());
//        }

//        [Fact]
//        public void Process_CanHandleFormattableString() {
//            var result = CreateProcessor().Process("using System; public class C { public void M() { IFormattable f = $\"{42}\"; } }", new ProcessingOptions {
//                SourceLanguage = LanguageIdentifier.CSharp
//            });

//            Assert.NotNull(result);
//            Assert.True(result.IsSuccess, GetErrorString(result));
//        }

//        private static string GetErrorString(ProcessingResult result) {
//            return result.Diagnostics
//                .Aggregate(new StringBuilder("Errors:"), (builder, d) => builder.AppendLine().Append(d))
//                .ToString();
//        }

//        private string GetResourceContent(string name) {
//            var fullName = GetType().Namespace + ".TestCode." + name;
//            // ReSharper disable once AssignNullToNotNullAttribute
//            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName)) {
//                if (stream == null)
//                    throw new FileNotFoundException("Resource was not found.", fullName);

//                using (var reader = new StreamReader(stream)) {
//                    return reader.ReadToEnd();
//                }
//            }
//        }

//        private ProcessingOptions GetProcessingOptions(string resourceName, string content) {
//            var scriptMode = content.StartsWith("// script mode") || content.StartsWith("' Script Mode");
//            var fromTo = Path.GetExtension(resourceName).TrimStart('.').Split('2');

//            return new ProcessingOptions {
//                SourceLanguage = LanguageMap[fromTo[0]],
//                TargetLanguage = LanguageMap[fromTo[1]],
//                OptimizationsEnabled = true,
//                ScriptMode = scriptMode
//            };
//        }

//        private static CodeProcessor CreateProcessor() {
//            var builder = new ContainerBuilder();
//            builder.RegisterModule<CoreModule>();
//            builder.RegisterType<CodeProcessor>().AsSelf();
//            var container = builder.Build();

//            return container.Resolve<CodeProcessor>();
//        }
//    }
//}
