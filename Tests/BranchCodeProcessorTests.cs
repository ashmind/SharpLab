using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using AshMind.IO.Abstractions.Adapters;
using TryRoslyn.Core;
using TryRoslyn.Core.Processing;
using Xunit;
using Xunit.Extensions;

namespace TryRoslyn.Tests {
    public class BranchCodeProcessorTests {
        [Theory]
        [PropertyData("Branches")]
        public void Process_CanExecuteSimpleCode_InBranch(string branchName) {
            var processor = new BranchCodeProcessor(branchName, CreateBranchProvider(), new FileSystem());
            var result = processor.Process("public class X { public void M() {} }");

            Assert.NotNull(result);
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
