using System.Collections.Immutable;
using JetBrains.Annotations;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.FSharp.Advanced;
using SharpLab.Server.Common.Internal;

namespace SharpLab.Server.Common.Languages {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class FSharpAdapter : ILanguageAdapter {
        private readonly AssemblyReferenceDiscoveryTaskSource _referencedAssembliesTaskSource = new();
        private readonly IAssemblyPathCollector _assemblyPathCollector;

        public string LanguageName => LanguageNames.FSharp;
        public AssemblyReferenceDiscoveryTask AssemblyReferenceDiscoveryTask => _referencedAssembliesTaskSource.Task;

        public FSharpAdapter(IAssemblyPathCollector assemblyPathCollector) {
            _assemblyPathCollector = assemblyPathCollector;
        }

        public void SlowSetup(MirrorSharpOptions options) {
            options.EnableFSharp(o => {
                o.LangVersion = "preview";

                var referencedAssemblyPaths = _assemblyPathCollector.SlowGetAllAssemblyPathsIncludingReferences(
                    // Essential
                    "netstandard",
                    "System.Runtime",
                    "FSharp.Core",

                    // Runtime
                    "SharpLab.Runtime",

                    // Requested
                    "System.Collections.Immutable",
                    "System.Data",
                    "System.Runtime.CompilerServices.Unsafe",
                    "System.Runtime.Intrinsics",
                    "System.Text.Json",
                    "System.Web.HttpUtility",
                    "System.Xml.Linq"
                ).ToImmutableArray();
                _referencedAssembliesTaskSource.Complete(referencedAssemblyPaths);

                o.AssemblyReferencePaths = referencedAssemblyPaths;
                o.TargetProfile = "netstandard";
            });
        }

        public void SetOptimize([NotNull] IWorkSession session, [NotNull] string optimize) {
            var debug = optimize == Optimize.Debug;
            var fsharp = session.FSharp();
            fsharp.ProjectOptions = fsharp.ProjectOptions
                .WithOtherOptionDebug(debug)
                .WithOtherOptionOptimize(!debug)
                .WithOtherOptionDefine("DEBUG", debug);
        }

        public void SetOptionsForTarget([NotNull] IWorkSession session, [NotNull] string target) {
            // I don't use `exe` for Run, see FSharpEntryPointRewriter
        }

        public ImmutableArray<int> GetMethodParameterLines(IWorkSession session, int lineInMethod, int columnInMethod) {
            return ImmutableArray<int>.Empty; // not supported yet
        }

        public ImmutableArray<string?> GetCallArgumentIdentifiers([NotNull] IWorkSession session, int callStartLine, int callStartColumn) {
            return ImmutableArray<string?>.Empty; // not supported yet
        }
    }
}