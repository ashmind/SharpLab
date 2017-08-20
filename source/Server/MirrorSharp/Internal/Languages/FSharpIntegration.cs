using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.FSharp.Advanced;
using SharpLab.Runtime;

namespace SharpLab.Server.MirrorSharp.Internal.Languages {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class FSharpIntegration : ILanguageIntegration {
        public string LanguageName => LanguageNames.FSharp;

        public void SlowSetup(MirrorSharpOptions options) {
            options.EnableFSharp(o => o.AssemblyReferencePaths = o.AssemblyReferencePaths.AddRange(new[] {
                // Essential
                typeof(TaskExtensions).Assembly.Location,

                // Runtime
                typeof(JitGenericAttribute).Assembly.Location,

                // Requested
                typeof(IDataReader).Assembly.Location // System.Data
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
    }
}