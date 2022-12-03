using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pedantic.IO;
using SharpLab.Server.Common;
using Xunit;
using Xunit.Abstractions;

namespace SharpLab.Tests.Internal {
    public class TestCode {
        private static readonly IReadOnlyDictionary<string, string> LanguageAndTargetMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "cs",     LanguageNames.CSharp },
            { "vb",     LanguageNames.VisualBasic },
            { "fs",     LanguageNames.FSharp },
            { "il",     TargetNames.IL },
            { "asm",    TargetNames.JitAsm },
            { "ast",    TargetNames.Ast },
            { "output", TargetNames.Run },
        };

        private static readonly IReadOnlyDictionary<string, (string start, string end)> CommentMarkers = new Dictionary<string, (string start, string end)>(StringComparer.OrdinalIgnoreCase) {
            { LanguageNames.CSharp, ("/*", "*/") },
            { LanguageNames.VisualBasic, ("/*", "*/") }, // TODO: Sort out
            { LanguageNames.FSharp, ("(*", "*)") },
            { LanguageNames.IL, ("/*", "*/") },
        };

        private static readonly bool ShouldUpdateOnAssert = Environment.GetEnvironmentVariable("SHARPLAB_TEST_UPDATE_SNAPSHOTS") == "true";

        public string Original { get; }
        public string SourceLanguageName { get; }
        public string TargetName { get; }

        private readonly string _expected;
        private readonly string? _snapshotFilePath;

        public TestCode(string original, string expected, string sourceLanguageName, string targetName, string? snapshotFilePath = null) {
            Original = original;
            SourceLanguageName = sourceLanguageName;
            TargetName = targetName;
            _expected = expected;
            _snapshotFilePath = snapshotFilePath;
        }

        public static TestCode FromFile(string relativePath, [CallerFilePath] string callerFilePath = "") {
            var testBasePath = Path.GetDirectoryName(callerFilePath)!;
            var fullPath = Path.Combine(AppContext.BaseDirectory, testBasePath, "TestCode", relativePath);

            var content = File.ReadAllText(fullPath);
            var extension = Path.GetExtension(relativePath);

            return FromContent(content, extension, fullPath);
        }

        public static TestCode FromResource(string name) {
            var content = EmbeddedResource.ReadAllText(typeof(ExecutionTests), "TestCode." + name);
            var extension = Path.GetExtension(name);

            return FromContent(content, extension);
        }

        private static TestCode FromContent(string content, string extension, string? sourceFilePath = null) {
            if (extension.Contains("2"))
                return FromContentFormatV1(content, extension);

            var split = Regex.Matches(content, @"[/(]\* (?<to>\S+)").Cast<Match>().Last();
            var from = LanguageAndTargetMap[extension.TrimStart('.')];
            var to = LanguageAndTargetMap[split.Groups["to"].Value];

            var code = content.Substring(0, split.Index).Trim();
            var expected = Regex.Replace(
                content.Substring(split.Index + split.Value.Length),
                @"^\s+|\s*\*[/)]\s*$", ""
            );

            return new TestCode(code, expected, from, to, sourceFilePath);
        }

        private static TestCode FromContentFormatV1(string content, string extension) {
            var parts = content.Split(new[] { "#=>" }, StringSplitOptions.None);
            var code = parts[0].Trim();
            var expected = parts[1].Trim();
            // ReSharper disable once PossibleNullReferenceException
            var fromTo = extension.TrimStart('.').Split('2').Select(x => LanguageAndTargetMap[x]).ToList();

            return new TestCode(code, expected, fromTo[0], fromTo[1]);
        }

        public async Task AssertIsExpectedAsync(string? result, ITestOutputHelper output) {
            var cleanResult = RemoveNonDeterminism(result?.Trim());
            output.WriteLine(cleanResult ?? "<null>");

            if (_snapshotFilePath is { } path && cleanResult is { } actual && ShouldUpdateOnAssert) {
                await UpdateFileAsync(path, actual);
                return;
            }

            Assert.Equal(NormalizeNewLines(_expected), NormalizeNewLines(cleanResult));
        }

        private string? RemoveNonDeterminism(string? result) {
            if (result == null)
                return null;

            result = Regex.Replace(result, @"0x[\dA-Fa-f]{7,16}(?=$|[^\dA-Fa-f])", "0x<IGNORE>");

            if (TargetName == TargetNames.JitAsm)
                result = Regex.Replace(result, @"CLR [\d\.]+", "CLR <IGNORE>");

            if (TargetName == TargetNames.Run) {
                // we need to ignore type handle in memory inspection output
                var pattern = @"(?<prefix>""inspection:memory"",.+""data"":\s*\[\s*)"
                    + @"(?<header>(?:\d+,\s*){" + IntPtr.Size + "})"
                    + @"(?<typeHandle>(?:\d+,\s*){" + IntPtr.Size + "})";
                result = Regex.Replace(result, pattern, m => {
                    var ignored = Regex.Replace(m.Groups["typeHandle"].Value, @"\d+", "<IGNORE>");
                    return m.Groups["prefix"].Value
                         + m.Groups["header"].Value
                         + ignored;
                }, RegexOptions.Singleline);

                // ignoring paths in exception stack traces. can't use <IGNORE> as paths are simply not present in Release mode
                result = Regex.Replace(result, @" in (?:/[A-Za-z]{2,}/|[A-Za-z]:[/\\])[^\r\n]+", "");
            }

            return result;
        }

        private string? NormalizeNewLines(string? value) {
            return value?.Replace("\r\n", "\n");
        }

        private async Task UpdateFileAsync(string path, string actual) {
            var (commentStart, commentEnd) = CommentMarkers[SourceLanguageName];
            var targetExtension = LanguageAndTargetMap.First(p => p.Value == TargetName).Key;

            var updatedContent = $"{Original}\r\n\r\n{commentStart} {targetExtension}\r\n\r\n{actual}\r\n\r\n{commentEnd}";
            using (var writer = new StreamWriter(path)) {
                await writer.WriteAsync(updatedContent);
            }
        }
    }
}