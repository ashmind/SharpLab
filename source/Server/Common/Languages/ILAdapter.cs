using System.Collections.Immutable;
using JetBrains.Annotations;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.IL.Advanced;
using Mobius.ILasm.Core;
using SharpLab.Server.Common.Internal;

namespace SharpLab.Server.Common.Languages {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class ILAdapter : ILanguageAdapter {
        private readonly AssemblyReferenceDiscoveryTaskSource _referencedAssembliesTaskSource = new();

        public string LanguageName => "IL";

        public void SlowSetup(MirrorSharpOptions options) {
            options.EnableIL();
        }

        public void SetOptimize(IWorkSession session, string optimize) {
        }

        public void SetOptionsForTarget(IWorkSession session, string target) {
            session.IL().Target = target == TargetNames.Run ? Driver.Target.Exe : Driver.Target.Dll;
        }

        public ImmutableArray<int> GetMethodParameterLines(IWorkSession session, int lineInMethod, int columnInMethod) {
            return ImmutableArray<int>.Empty; // not supported yet
        }

        public ImmutableArray<string?> GetCallArgumentIdentifiers(IWorkSession session, int callStartLine, int callStartColumn) {
            return ImmutableArray<string?>.Empty; // not supported yet
        }

        public AssemblyReferenceDiscoveryTask AssemblyReferenceDiscoveryTask => _referencedAssembliesTaskSource.Task;
    }
}
