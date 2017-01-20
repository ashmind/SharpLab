using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using AssemblyResolver.Common;

namespace AssemblyResolver.Steps {
    public static class Step1 {
        public static void CollectMainAssemblies(string binariesDirectoryPath, out IImmutableDictionary<AssemblyShortName, AssemblyDetails> mainAssemblies) {
            FluentConsole.White.Line($"Scanning {binariesDirectoryPath}…");
            var mainAssembliesBuilder = ImmutableSortedDictionary.CreateBuilder<AssemblyShortName, AssemblyDetails>();
            foreach (var assemblyPath in Directory.EnumerateFiles(binariesDirectoryPath, "*.*")) {
                if (!Regex.IsMatch(assemblyPath, @"(?:\.dll|\.exe)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    continue;

                FluentConsole.Gray.Line($"  {Path.GetFileName(assemblyPath)}");
                var name = Path.GetFileNameWithoutExtension(assemblyPath);
                // ReSharper disable once AssignNullToNotNullAttribute
                mainAssembliesBuilder.Add(name, AssemblyDetails.ReadFrom(assemblyPath, readSymbols: false));
            }
            mainAssemblies = mainAssembliesBuilder.ToImmutable();
        }

    }
}
