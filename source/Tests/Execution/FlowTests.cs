using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using SharpLab.Tests.Execution.Internal;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Execution {
    [Collection(TestCollectionNames.Execution)]
    public class FlowTests {
        [Theory]
        [InlineData("Variable.AssignCall.cs")]
        [InlineData("Variable.ManyVariables.cs")]
        [InlineData("Return.Simple.cs")]
        [InlineData("Return.Ref.cs")]
        [InlineData("Return.Ref.Readonly.cs")]
        [InlineData("Loop.For.10Iterations.cs")]
        [InlineData("Variable.MultipleDeclarationsOnTheSameLine.cs")]
        [InlineData("Variable.LongName.cs")]
        [InlineData("Variable.LongValue.cs")]
        [InlineData("Variable.LongValue.UnicodeCharBreak.cs")]
        [InlineData("Regression.ToStringNull.cs")] // https://github.com/ashmind/SharpLab/issues/380
        [InlineData("Variable.Array.cs")]
        public async Task Flow_IncludesExpectedValues(string codeFilePath) {
            var code = await TestCode.FromCodeOnlyFileAsync("Flow/Values/" + codeFilePath);
            var cleanCodeLines = code.Split("\r\n")
                .Select(line => Regex.Replace(line, @"//.+$", ""));

            var output = await ContainerTestDriver.CompileAndExecuteAsync(code);
            var valuesByLineNumber = ExtractAndGroupFlowValueStepsByLineNumber(output);
            var resultCode = string.Join("\r\n", cleanCodeLines.Select(
                (line, index) => line + (valuesByLineNumber.TryGetValue(index + 1, out var values) ? $"// [{values}]" : "")
            ));

            Assert.Equal(code, resultCode);
        }

        private IReadOnlyDictionary<int, string> ExtractAndGroupFlowValueStepsByLineNumber(string output) {
            var flowJsonArray = Regex.Matches(output, @"^#\{.+", RegexOptions.Multiline)
                .Select(m => m.Value.TrimStart('#'))
                .Select(json => JsonDocument.Parse(json))
                .Select(json => json.RootElement.TryGetProperty("flow", out var flowArray) ? flowArray : (JsonElement?)null)
                .First(a => a != null)
                !.Value;

            var valuesByLineNumber = new Dictionary<int, IList<IList<string>>>();
            IList<IList<string>> GetOrAddValues(int lineNumber) {
                if (!valuesByLineNumber!.TryGetValue(lineNumber, out var values)) {
                    values = new List<IList<string>>();
                    valuesByLineNumber.Add(lineNumber, values);
                }
                return values;
            }

            foreach (var item in flowJsonArray.EnumerateArray()) {
                if (item.ValueKind == JsonValueKind.Number) { // non-value step
                    GetOrAddValues(item.GetInt32()).Add(new List<string>());
                    continue;
                }

                if (item.ValueKind != JsonValueKind.Array)
                    continue;

                var values = GetOrAddValues(item[0].GetInt32());
                var name = item.GetArrayLength() > 2 ? item[2].GetString() : null;
                var value = item[1].ValueKind == JsonValueKind.String
                    ? item[1].GetString()!
                    : item[1].GetInt32().ToString();

                if (values.Count == 0)
                    values.Add(new List<string>());

                values.Last().Add(name != null ? $"{name}: {value}" : value);
            }

            return valuesByLineNumber
                .Where(p => p.Value.Any(v => v.Any()))
                .ToDictionary(
                    p => p.Key,
                    p => string.Join("; ", p.Value.Where(v => v.Any()).Select(v => string.Join(", ", v)))
                );
        }
    }
}
