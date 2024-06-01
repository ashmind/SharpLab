using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using SharpLab.Server.Common.Diagnostics;
using Xunit.Abstractions;

namespace SharpLab.Tests.Internal;

public static class TestDiagnosticLog {
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

        string SafePath(string name) => Regex.Replace(name, @"[^a-zA-Z\d._\-]+", "_");
        var safeTestName = SafePath(testName);
        if (safeTestName.Length > 100)
            safeTestName = safeTestName.Substring(0, 100) + "-" + safeTestName.GetHashCode();

        var basePath = Path.Combine(
            AppContext.BaseDirectory, "assembly-log",
            testType.Name, safeTestName
        );
        #if DEBUG
        DiagnosticLog.Enable(
            output.WriteLine,
            stepName => Path.Combine(basePath, SafePath(stepName))
        );
        #endif
    }
}
