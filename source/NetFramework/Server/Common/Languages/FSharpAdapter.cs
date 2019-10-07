using System.Collections.Immutable;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.FSharp.Core;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.FSharp.Advanced;
using SharpLab.Runtime;
using SharpLab.Server.Common.Internal;
using SharpLab.Server.Compilation.Internal;

namespace SharpLab.Server.Common.Languages {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class FSharpAdapter : ILanguageAdapter {
        private AssemblyReferenceDiscoveryTaskSource _referencedAssembliesTaskSource = new AssemblyReferenceDiscoveryTaskSource();
        private readonly IAssemblyReferenceCollector _referenceCollector;

        public string LanguageName => LanguageNames.FSharp;
        public AssemblyReferenceDiscoveryTask AssemblyReferenceDiscoveryTask => _referencedAssembliesTaskSource.Task;

        public FSharpAdapter(IAssemblyReferenceCollector referenceCollector) {
            _referenceCollector = referenceCollector;
        }

        public void SlowSetup(MirrorSharpOptions options) {
            options.EnableFSharp(o => {
                var assemblyOfObject = typeof(object).Assembly;
                var referencedAssemblies = _referenceCollector.SlowGetAllReferencedAssembliesRecursive(
                    // Essential
                    assemblyOfObject,
                    NetFrameworkRuntime.AssemblyOfValueTask,
                    typeof(TaskExtensions).Assembly,
                    typeof(FSharpOption<>).Assembly,

                    // Runtime
                    typeof(JitGenericAttribute).Assembly,

                    // Requested
                    typeof(XDocument).Assembly, // System.Xml.Linq
                    typeof(IDataReader).Assembly, // System.Data
                    typeof(HttpUtility).Assembly // System.Web
                );

                var referencedAssemblyPaths = referencedAssemblies.Select(a => a.Location).ToImmutableArray();
                if (assemblyOfObject.GetName().Name != "mscorlib") {
                    var mscorlibPath = Path.Combine(Path.GetDirectoryName(assemblyOfObject.Location)!, "mscorlib.dll");
                    referencedAssemblyPaths = referencedAssemblyPaths.Add(mscorlibPath);
                }
                _referencedAssembliesTaskSource.Complete(referencedAssemblyPaths);
                o.AssemblyReferencePaths = referencedAssemblyPaths;
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