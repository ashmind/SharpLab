using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Sdk;

namespace TryRoslyn.Tests {
    public static class AssertGold {
        public static void Equal(string expected, string actual) {
            try {
                Assert.Equal(expected, actual);
            }
            catch (AssertException ex) {
                var test = GetCurrentTestMethod();
                var message = string.Join(
                    Environment.NewLine,
                    ex.Message,
                    "The following allows R# DiffGold plugin to be used:",
                    ReportTempFile("GoldFile", expected, test),
                    ReportTempFile("TempFile", actual, test)
                );
                throw new AssertException(message);
            }
        }

        private static MethodBase GetCurrentTestMethod() {
            var stackFrames = new StackTrace().GetFrames();
            return stackFrames.Select(f => f.GetMethod()).First(m => m.GetCustomAttributes(false).Any(a => a is FactAttribute));
        }

        private static string ReportTempFile(string type, string content, MethodBase currentTest) {
            var safeTestName = Regex.Replace(currentTest.Name, @"[^A-Za-z\.]", "_");
            var fileName = string.Format("{0}-{1}.tmp", safeTestName, type);
            File.WriteAllText(fileName, content);
            var fullPath = Path.GetFullPath(fileName);

            return string.Format("Data.{0} = {1}#:)", type, new Uri(fullPath));
        }
    }
}
