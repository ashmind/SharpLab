using CommandLine;

namespace AssemblyResolver {
    public class Arguments {
        [Option("source-bin", Required = true)]
        public string SourceProjectAssemblyDirectoryPath { get; set; }

        [Option("roslyn-bin", Required = true)]
        public string RoslynBinariesDirectoryPath { get; set; }

        [Option("target", Required = true)]
        public string TargetDirectoryPath { get; set; }

        [Option("target-app-config", Required = true)]
        public string TargetApplicationConfigurationPath { get; set; }
    }
}
