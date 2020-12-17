using System.Collections.Immutable;
using MirrorSharp;
using MirrorSharp.Advanced;
using SharpLab.Server.Common.Internal;

namespace SharpLab.Server.Common {
    public interface ILanguageAdapter {
        string LanguageName { get; }

        void SlowSetup(MirrorSharpOptions options);
        void SetOptimize(IWorkSession session, string optimize);
        void SetOptionsForTarget(IWorkSession session, string target);

        ImmutableArray<int> GetMethodParameterLines(IWorkSession session, int lineInMethod, int columnInMethod);
        ImmutableArray<string?> GetCallArgumentIdentifiers(IWorkSession session, int callStartLine, int callStartColumn);

        // Note: in some cases this Task is never resolved (e.g. if VB is never used)
        AssemblyReferenceDiscoveryTask AssemblyReferenceDiscoveryTask { get; }
    }
}