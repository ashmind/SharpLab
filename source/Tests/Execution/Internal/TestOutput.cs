using System.Text.RegularExpressions;

namespace SharpLab.Tests.Execution.Internal {
    public static class TestOutput {
        public static string RemoveFlowJson(string output) {
            return Regex.Replace(output, "#\\{\"flow\".+$", "", RegexOptions.Singleline);
        }
    }
}
