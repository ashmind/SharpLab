using System;
using System.Collections.Immutable;
using System.Data;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.FSharp.Advanced;
using SharpLab.Runtime;
using SharpLab.Server.Compilation.Internal;

namespace SharpLab.Server.Common.Languages {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class FSharpAdapter : ILanguageAdapter {
        public string LanguageName => LanguageNames.FSharp;

        public void SlowSetup(MirrorSharpOptions options) {
            options.EnableFSharp(o => o.AssemblyReferencePaths = o.AssemblyReferencePaths.AddRange(new[] {
                // Essential
                NetFrameworkRuntime.AssemblyOfValueTask.Location,
                typeof(TaskExtensions).Assembly.Location,

                // Runtime
                typeof(JitGenericAttribute).Assembly.Location,

                // Requested
                typeof(XDocument).Assembly.Location, // System.Xml.Linq
                typeof(IDataReader).Assembly.Location, // System.Data
                typeof(HttpUtility).Assembly.Location // System.Web
            }));
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