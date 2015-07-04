using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using AshMind.Extensions;
using AshMind.IO.Abstractions.Adapters;
using Microsoft.CodeAnalysis;
using TryRoslyn.Core;
using TryRoslyn.Core.Processing;
using Xunit;
using Xunit.Extensions;

namespace TryRoslyn.Tests {
    public class BranchCodeProcessorTests : IDisposable {
        [Theory]
        [MemberData("Branches")]
        public void Process_CanHandleSimpleCSharpCode_InBranch(string branchName) {
            var processor = CreateProcessor(branchName);
            var result = processor.Process("public class X { public void M() {} }");

            Assert.NotNull(result);
        }

        [Theory]
        [MemberData("Branches")]
        public void Process_CanHandleSimpleVBNetCode_InBranch(string branchName) {
            var processor = CreateProcessor(branchName);
            var result = processor.Process("Public Class C\r\nEnd Class", new ProcessingOptions {
                SourceLanguage = LanguageIdentifier.VBNet
            });

            Assert.NotNull(result);
        }

        [Fact]
        public void Process_CanHandleFormattableString_InMaster() {
            var processor = CreateProcessor("master");
            var result = processor.Process("using System; public class C { public void M() { IFormattable f = $\"{42}\"; } }", new ProcessingOptions {
                SourceLanguage = LanguageIdentifier.CSharp
            });

            Assert.NotNull(result);
            Assert.True(result.IsSuccess, GetErrorString(result));
        }

        private static string GetErrorString(ProcessingResult result) {
            return result.Diagnostics
                .Aggregate(new StringBuilder("Errors:"), (builder, d) => builder.AppendLine().Append(d))
                .ToString();
        }

        public static IEnumerable<object[]> Branches {
            get {
                var names = CreateProvider().GetBranches().Select(b => b.Name).ToArray();
                Assert.True(names.Contains("master"), "Branch 'master' does not exist: please run BuildRoslyn.ps1 before this test.");
                return names.Select(n => new object[] { n });
            }
        }

        private BranchCodeProcessor CreateProcessor(string branchName) {
            var processor = new BranchCodeProcessor(branchName, CreateProvider(), new FileSystem());
            _disposables.Add(processor);
            return processor;
        }
        
        private static BranchProvider CreateProvider() {
            return new BranchProvider(new DirectoryInfoAdapter(new DirectoryInfo(ConfigurationManager.AppSettings["BinariesRoot"])));
        }

        private readonly ICollection<IDisposable> _disposables = new Collection<IDisposable>();
        public void Dispose() {
            _disposables.ForEach(d => d.Dispose());
        }
    }
}
