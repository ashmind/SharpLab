using System;
using System.Collections.Generic;
using System.Linq;
using AshMind.IO.Abstractions.Mocks;
using TryRoslyn.Core;
using Xunit;

namespace TryRoslyn.Tests {
    public class BranchProviderTests {
        [Fact]
        public void GetBranches_ReturnsLastCommitInfo() {
            var message = "Initial support for auto property initializers.\r\n\r\nFeatures the new syntax ('= expr;'), new rules around required accessors, and allows auto property and field initializers in structs.";
            var text = "b02c98c7906dd3666f479699b83760ce239d6d2d 2014-04-16 Andy Gocke\r\n" + message;
            var directory = new DirectoryMock("root", 
                new DirectoryMock("test",
                    new FileMock("LastCommit.txt", text)
                )
            );

            var provider = new BranchProvider(directory);
            var branch = Assert.Single(provider.GetBranches());

            Assert.Equal("b02c98c7906dd3666f479699b83760ce239d6d2d", branch.LastCommitHash);
            Assert.Equal("2014-04-16", branch.LastCommitDate.ToString("yyyy-MM-dd"));
            Assert.Equal("Andy Gocke", branch.LastCommitAuthor);
            Assert.Equal(message, branch.LastCommitMessage);
        }
    }
}
