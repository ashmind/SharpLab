using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Reflection;
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
        private ReferencedAssembliesLoadTaskSource _referencedAssembliesTaskSource = new ReferencedAssembliesLoadTaskSource();

        public string LanguageName => LanguageNames.FSharp;
        public ReferencedAssembliesLoadTask ReferencedAssembliesTask => _referencedAssembliesTaskSource.Task;

        public void SlowSetup(MirrorSharpOptions options) {
            options.EnableFSharp(o => {
                var referencedAssemblies = ImmutableList.Create(
                    // Essential
                    typeof(object).Assembly,
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
                _referencedAssembliesTaskSource.Complete(referencedAssemblies);
                o.AssemblyReferencePaths = referencedAssemblies.Select(a => a.Location).ToImmutableArray();
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
    }
}