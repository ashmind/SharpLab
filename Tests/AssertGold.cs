using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace TryRoslyn.Tests {
    public static class AssertGold {
        public static void Equal(string expected, string actual) {
            try {
                Assert.Equal(expected, actual);
            }
            catch (AssertException ex) {
                var testId = Guid.NewGuid();
                var message = string.Join(
                    Environment.NewLine,
                    ex.Message,
                    "The following allows R# DiffGold plugin to be used:",
                    ReportTempFile("GoldFile", expected, testId),
                    ReportTempFile("TempFile", actual, testId)
                );
                throw new AssertException(message);
            }
        }
        
        private static string ReportTempFile(string type, string content, Guid testId) {
            var fileName = string.Format("{0:D}-{1}.tmp", testId, type);
            File.WriteAllText(fileName, content);
            var fullPath = Path.GetFullPath(fileName);

            return string.Format("Data.{0} = {1}#:)", type, new Uri(fullPath));
        }
    }
}
