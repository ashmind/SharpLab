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

        public static void AssertFlowMatchesValueComments(string code, string output, ITestOutputHelper testOutputHelper) {
            AssertFlowMatchesComments(code, output, AssertCommentMode.Values, testOutputHelper);
        }
        
        public static void AssertFlowMatchesJumpComments(string code, string output, ITestOutputHelper testOutputHelper) {
            AssertFlowMatchesComments(code, output, AssertCommentMode.Jumps, testOutputHelper);
        }

        private static void AssertFlowMatchesComments(string code, string output, AssertCommentMode mode, ITestOutputHelper testOutputHelper) {
            var actual = ApplyActualFlowAsComments(code, output, mode);
            testOutputHelper.WriteLine(actual);
            Assert.Equal(code, actual);
        }

        private static string ApplyActualFlowAsComments(string code, string output, AssertCommentMode mode) {
            var cleanCodeLines = code.Split("\r\n")
                .Select(line => Regex.Replace(line, @"//.+$", ""));

            var notesByLineNumber = ExtractAndGroupFlowNotesForComments(output, mode);
            return string.Join("\r\n", cleanCodeLines.Select(
                (line, index) => line + (notesByLineNumber.TryGetValue(index + 1, out var note) ? $"// {note}" : "")
            ));
        }

        private static IReadOnlyDictionary<int, string> ExtractAndGroupFlowNotesForComments(string output, AssertCommentMode mode) {
            var groupsByLineNumber = new Dictionary<int, IList<LineNoteGroup>>();
            IList<LineNoteGroup> GetOrAddNoteGroups(int lineNumber) {
                if (!groupsByLineNumber!.TryGetValue(lineNumber, out var groups)) {
                    groups = new List<LineNoteGroup> { new LineNoteGroup() };
                    groupsByLineNumber.Add(lineNumber, groups);
                }
                return groups;
            }
            
            var lastLineNumber = (int?)null;
            foreach (var step in ExtractFlowSteps(output)) {
                switch (step) {
                    case LineStep l:
                        lastLineNumber = l.LineNumber;
                        GetOrAddNoteGroups(l.LineNumber).Add(new());
                        break;
                        
                    case ValueStep v when mode == AssertCommentMode.Values: {
                        lastLineNumber = v.LineNumber;
                        var notesLists = GetOrAddNoteGroups(v.LineNumber);
                        GetOrAddNoteGroups(v.LineNumber).Last()
                            .Values.Add(v.Name != null ? $"{v.Name}: {v.Value}" : v.Value);
                        break;
                    }

                    case JumpStep j when mode == AssertCommentMode.Jumps && lastLineNumber != null: {
                        GetOrAddNoteGroups(lastLineNumber.Value).Last().HasJump = true;
                        break;
                    }

                    // TODO: Add mode for area tests
                    /*
                    case AreaReport a when ???: {
                        var start = GetOrAddNotesLists(a.StartLineNumber);
                        start.Add(new List<string> { a.Type + " start" });
                        start.Add(new List<string>());

                        var end = GetOrAddNotesLists(a.EndLineNumber);
                        end.Add(new List<string> { a.Type + " end" });
                        end.Add(new List<string>());
                        break;
                    }
                    */
                }

            }

            return groupsByLineNumber
                .Select(gs => (
                    line: gs.Key,
                    notes: string.Join(" ", gs.Value.Where(g => !g.IsEmpty).Select(g => g.ToString()))
                ))
                .Where(x => x.notes.Length > 0)
                .ToDictionary(
                    x => x.line,
                    x => x.notes
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
                        yield return new LineStep(lineNumber);
                        break;
                    }

                    case JsonValueKind.String when item.GetString() == "j": {
                        yield return new JumpStep();
                        break;
                    }

                    case JsonValueKind.Object: {
                        var exception = item.GetProperty("exception").GetString()!;
                        yield return new ValueStep(lastLineNumber, exception, "exception");
                        break;
                    }

                    case JsonValueKind.Array when item[0].ValueKind == JsonValueKind.Number: {
                        var lineNumber = item[0].GetInt32();
                        var name = item.GetArrayLength() > 2 ? item[2].GetString() : null;
                        var value = item[1].ValueKind == JsonValueKind.String
                            ? item[1].GetString()!
                            : item[1].GetInt32().ToString();

                        yield return new ValueStep(lineNumber, value, name);
                        break;
                    }
                        
                    case JsonValueKind.Array when item[0].ValueKind == JsonValueKind.String: {
                        var type = item[0].GetString()! switch {
                            "l" => "loop",
                            "m" => "method",
                            var t => throw new ("Unknown area type: " + t)
                        };
                        var startLineNumber = item[1].GetInt32();
                        var endLineNumber = item[2].GetInt32();

                        yield return new AreaReport(type, startLineNumber, endLineNumber);
                        break;
                    }

                    default:
                        throw new($"Unknown step value kind: {item}");
                }
            }
        }

        private record FlowStep();
        private record LineStep(int LineNumber): FlowStep;
        private record JumpStep() : FlowStep;
        private record ValueStep(int LineNumber, string Value, string? Name) : FlowStep;
        private record AreaReport(string Type, int StartLineNumber, int EndLineNumber) : FlowStep;

        private enum AssertCommentMode {
            Values,
            Jumps
        }

        private class LineNoteGroup {
            public IList<string> Values { get; } = new List<string>();
            public bool HasJump { get; set; }
            public bool IsEmpty => !Values.Any() && !HasJump;
            public override string ToString() => string.Join(" ", new[] {
                Values.Any() ? $"[{string.Join(", ", Values)}]" : null,
                HasJump ? "jump🠊" : null
            }.Where(p => p != null));
        }
    }
}
