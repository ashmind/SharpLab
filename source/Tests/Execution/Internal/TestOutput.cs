using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace SharpLab.Tests.Execution.Internal {
    public static class TestOutput {
        public static string RemoveFlowJson(string output) {
            return Regex.Replace(output, "#\\{\"flow\".+$", "", RegexOptions.Singleline);
        }

        public static void AssertFlowMatchesComments(string code, string output, ITestOutputHelper testOutputHelper) {
            var actual = ApplyActualFlowAsComments(code, output);
            testOutputHelper.WriteLine(actual);
            Assert.Equal(code, actual);
        }

        private static string ApplyActualFlowAsComments(string code, string output) {
            var cleanCodeLines = code.Split("\r\n")
                .Select(line => Regex.Replace(line, @"//.+$", ""));

            var valuesByLineNumber = ExtractAndGroupFlowValueStepsByLineNumber(output);
            return string.Join("\r\n", cleanCodeLines.Select(
                (line, index) => line + (valuesByLineNumber.TryGetValue(index + 1, out var values) ? $"// [{values}]" : "")
            ));
        }

        private static IReadOnlyDictionary<int, string> ExtractAndGroupFlowValueStepsByLineNumber(string output) {
            var valuesByLineNumber = new Dictionary<int, IList<IList<string>>>();
            IList<IList<string>> GetOrAddValues(int lineNumber) {
                if (!valuesByLineNumber!.TryGetValue(lineNumber, out var values)) {
                    values = new List<IList<string>>();
                    valuesByLineNumber.Add(lineNumber, values);
                }
                return values;
            }

            foreach (var step in ExtractFlowSteps(output)) {
                if (step.Value == null) { // non-value step
                    GetOrAddValues(step.LineNumber).Add(new List<string>());
                    continue;
                }

                var values = GetOrAddValues(step.LineNumber);
                if (values.Count == 0)
                    values.Add(new List<string>());

                values.Last().Add(step.Name != null ? $"{step.Name}: {step.Value}" : step.Value);
            }

            return valuesByLineNumber
                .Where(p => p.Value.Any(v => v.Any()))
                .ToDictionary(
                    p => p.Key,
                    p => string.Join("; ", p.Value.Where(v => v.Any()).Select(v => string.Join(", ", v)))
                );
        }

        private static IEnumerable<FlowStep> ExtractFlowSteps(string output) {
            var flowJsonArray = Regex.Matches(output, @"^#\{.+", RegexOptions.Multiline)
                .Select(m => m.Value.TrimStart('#'))
                .Select(json => JsonDocument.Parse(json))
                .Select(json => json.RootElement.TryGetProperty("flow", out var flowArray) ? flowArray : (JsonElement?)null)
                .First(a => a != null)
                !.Value;

            var lastLineNumber = -1;
            foreach (var item in flowJsonArray.EnumerateArray()) {
                switch (item.ValueKind) {
                    case JsonValueKind.Number: {
                        // non-value step
                        var lineNumber = item.GetInt32();
                        lastLineNumber = lineNumber;
                        yield return new(lineNumber);
                        break;
                    }

                    case JsonValueKind.Object: {
                        var exception = item.GetProperty("exception").GetString();
                        yield return new(lastLineNumber, exception, "exception");
                        break;
                    }

                    case JsonValueKind.Array: {
                        var lineNumber = item[0].GetInt32();
                        var name = item.GetArrayLength() > 2 ? item[2].GetString() : null;
                        var value = item[1].ValueKind == JsonValueKind.String
                            ? item[1].GetString()!
                            : item[1].GetInt32().ToString();

                        yield return new(lineNumber, value, name);
                        break;
                    }

                    default:
                        throw new($"Unknown step value kind: ${item}");
                }
            }
        }

        private record FlowStep(int LineNumber, string? Value = null, string? Name = null);
    }
}
