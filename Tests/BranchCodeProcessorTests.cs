using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using AshMind.IO.Abstractions.Adapters;
using TryRoslyn.Core;
using TryRoslyn.Core.Processing;
using Xunit;
using Xunit.Extensions;

namespace TryRoslyn.Tests {
    // note that these tests fail when run all together
    // please fix it if you have time to do so
    public class BranchCodeProcessorTests {
        [Theory]
        [PropertyData("Branches")]
        public void Process_CanHandleSimpleCSharpCode_InBranch(string branchName) {
            var processor = new BranchCodeProcessor(branchName, CreateBranchProvider(), new FileSystem());
            var result = processor.Process("public class X { public void M() {} }");

            Assert.NotNull(result);
        }

        [Fact]
        public void Process_CanHandlePrimaryConstructors_InMaster() {
            var processor = new BranchCodeProcessor("master", CreateBranchProvider(), new FileSystem());
            var result = processor.Process("public class X(int v) {}");

            Assert.True(result.IsSuccess, GetErrorString(result));
        }

        [Theory]
        [PropertyData("Branches")]
        public void Process_CanHandleSimpleVBNetCode_InBranch(string branchName) {
            var processor = new BranchCodeProcessor(branchName, CreateBranchProvider(), new FileSystem());
            var result = processor.Process("Public Class C\r\nEnd Class", new ProcessingOptions {
                SourceLanguage = LanguageIdentifier.VBNet
            });

            Assert.NotNull(result);
        }

        private static string GetErrorString(ProcessingResult result) {
            return result.Diagnostics
                .Aggregate(new StringBuilder("Errors:"), (builder, d) => builder.AppendLine().Append(d))
                .ToString();
        }

        public static IEnumerable<object[]> Branches {
            get {
                var names = CreateBranchProvider().GetBranches().Select(b => b.Name).ToArray();
                Assert.True(names.Contains("master"), "Branch 'master' does not exist: please run BuildRoslyn.ps1 before this test.");
                return names.Select(n => new object[] { n });
            }
        }
        
        private static BranchProvider CreateBranchProvider() {
            return new BranchProvider(new DirectoryInfoAdapter(new DirectoryInfo(ConfigurationManager.AppSettings["BinariesRoot"])));
        }
    }
}
