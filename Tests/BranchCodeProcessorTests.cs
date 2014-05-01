using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using TryRoslyn.Core;
using TryRoslyn.Core.Processing;
using Xunit;

namespace TryRoslyn.Tests {
    public class BranchCodeProcessorTests {
        [Fact]
        public void Process_CanExecuteSimpleCode_InOtherBranch() {
            EnsureBranchExists("master");
            var processor = new BranchCodeProcessor("master", CreateBranchProvider());
            var result = processor.Process("public class X { public void M() {} }");

            Assert.NotNull(result);
        }

        private void EnsureBranchExists(string branchName) {
            var branch = CreateBranchProvider().GetDirectory(branchName);
            Assert.True(branch.Exists, "Branch " + branch.FullName + " does not exist: please run BuildRoslyn.ps1 before this test.");
        }

        private BranchProvider CreateBranchProvider() {
            return new BranchProvider(new DirectoryInfo(ConfigurationManager.AppSettings["BinariesRoot"]));
        }
    }
}
