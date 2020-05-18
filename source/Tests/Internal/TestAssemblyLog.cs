using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using SharpLab.Server.Common.Diagnostics;
using Xunit.Abstractions;

namespace SharpLab.Tests.Internal {
    public static class TestAssemblyLog {
        [Conditional("DEBUG")]
        public static void Enable(ITestOutputHelper output) {
            var test = ((ITest)
                output
                    .GetType()
                    .GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(output)!
            );
            var testType = test.TestCase.TestMethod.TestClass.Class.ToRuntimeType();
            var testName = test.DisplayName.Replace(testType.FullName + ".", "");

            var safeTestName = Regex.Replace(testName, "[^a-zA-Z._-]+", "_");
            if (safeTestName.Length > 100)
                safeTestName = safeTestName.Substring(0, 100) + "-" + safeTestName.GetHashCode();

            var testPath = Path.Combine(
                AppContext.BaseDirectory, "assembly-log",
                testType.Name, safeTestName,
                "{0}.dll"
            );
            #if DEBUG
            AssemblyLog.Enable(testPath);
            #endif
        }
    }
}
