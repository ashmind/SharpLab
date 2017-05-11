using System;
using System.Collections.Immutable;
using AssemblyResolver.Common;
using AssemblyResolver.Steps;
using CommandLine;

namespace AssemblyResolver {
    public static class Program {
        public static int Main(string[] args) {
            var arguments = new Arguments();
            if (!Parser.Default.ParseArgumentsStrict(args, arguments))
                return -1;

            try {
                MainSafe(arguments);
                return 0;
            }
            catch (Exception ex) {
                FluentConsole.Red.Line(ex);
                return ex.HResult;
            }
        }

        private static void MainSafe(Arguments arguments) {
            var targetDirectoryPath = arguments.TargetDirectoryPath;

            IImmutableDictionary<AssemblyShortName, AssemblyDetails> mainAssemblies;
            IImmutableDictionary<AssemblyShortName, AssemblyDetails> usedRoslynAssemblies;
            IImmutableDictionary<AssemblyShortName, string> roslynAssemblyPaths;
            IImmutableDictionary<AssemblyShortName, IImmutableSet<PackageInfo>> roslynPackageMap;
            IImmutableDictionary<AssemblyShortName, AssemblyDetails> othersReferencedByRoslyn;

            Step1.CollectMainAssemblies(arguments.SourceProjectAssemblyDirectoryPath, out mainAssemblies);
            Step2.CollectRoslynAssemblies(arguments.RoslynBinariesDirectoryPath, ref mainAssemblies, out usedRoslynAssemblies, out roslynAssemblyPaths);
            Step3.CollectRoslynPackageReferences(arguments.RoslynBinariesDirectoryPath, out roslynPackageMap);
            Step4.CollectRoslynReferences(ref usedRoslynAssemblies, roslynAssemblyPaths, ref mainAssemblies, roslynPackageMap, out othersReferencedByRoslyn);

            Step5.CleanTargetDirectory(arguments.TargetDirectoryPath);

            Step6.CopyAssembliesReferencedByRoslyn(othersReferencedByRoslyn, targetDirectoryPath);
            Step7.CopyRoslynAssemblies(usedRoslynAssemblies, targetDirectoryPath);
            Step8.RewriteAndCopyMainAssemblies(mainAssemblies, targetDirectoryPath, usedRoslynAssemblies);
            Step9.UpdateBindingRedirects(arguments.TargetApplicationConfigurationPath, othersReferencedByRoslyn.Values);
        }
    }
}