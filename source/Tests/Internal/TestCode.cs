using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Pedantic.IO;
using SharpLab.Server.Common;
using Xunit;
using Xunit.Abstractions;

namespace SharpLab.Tests.Internal {
    public class TestCode {
        private static readonly IReadOnlyDictionary<string, string> LanguageAndTargetMap = new Dictionary<string, string> {
            { "cs",     LanguageNames.CSharp },
            { "vb",     LanguageNames.VisualBasic },
            { "fs",     LanguageNames.FSharp },
            { "il",     TargetNames.IL },
            { "asm",    TargetNames.JitAsm },
            { "ast",    TargetNames.Ast },
            { "output", TargetNames.Run },
        };

        public string Original { get; }
        public string SourceLanguageName { get; }
        public string TargetName { get; }

        private readonly string _expected;

        public TestCode(string original, string expected, string sourceLanguageName, string targetName) {
            Original = original;
            SourceLanguageName = sourceLanguageName;
            TargetName = targetName;
            _expected = expected;
        }

        public static TestCode FromResource(string name) {
            var content = EmbeddedResource.ReadAllText(typeof(DecompilationTests), "TestCode." + name);
            var parts = content.Split("#=>");
            var code = parts[0].Trim();
            var expected = parts[1].Trim();
            // ReSharper disable once PossibleNullReferenceException
            var fromTo = Path.GetExtension(name)!.TrimStart('.').Split('2').Select(x => LanguageAndTargetMap[x]).ToList();

            return new TestCode(code, expected, fromTo[0], fromTo[1]);
        }

        public void AssertIsExpected(string? result, ITestOutputHelper output) {
            var cleanResult = RemoveNonDeterminism(result?.Trim());
            output.WriteLine(cleanResult ?? "<null>");
            Assert.Equal(NormalizeNewLines(_expected), NormalizeNewLines(cleanResult));
        }

        private string? RemoveNonDeterminism(string? result) {
            if (result == null)
                return null;

            result = Regex.Replace(result, @"0x[\dA-Fa-f]{7,16}(?=$|[^\dA-Fa-f])", "0x<IGNORE>");

            if (TargetName == TargetNames.JitAsm)
                result = Regex.Replace(result, @"CLR v[\d\.]+", "CLR v<IGNORE>");

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
    }
}
