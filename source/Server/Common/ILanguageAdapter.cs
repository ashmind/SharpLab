using System.Collections.Immutable;
using JetBrains.Annotations;
using MirrorSharp;
using MirrorSharp.Advanced;
using SharpLab.Server.Common.Internal;

namespace SharpLab.Server.Common {
    public interface ILanguageAdapter {
        [NotNull] string LanguageName { get; }

        void SlowSetup([NotNull] MirrorSharpOptions options);
        void SetOptimize([NotNull] IWorkSession session, [NotNull] string optimize);
        void SetOptionsForTarget([NotNull] IWorkSession session, [NotNull] string target);

        ImmutableArray<int> GetMethodParameterLines([NotNull] IWorkSession session, int lineInMethod, int columnInMethod);
        ImmutableArray<string?> GetCallArgumentIdentifiers([NotNull] IWorkSession session, int callStartLine, int callStartColumn);

        // Note: in some cases this Task is never resolved (e.g. if VB is never used)
        AssemblyReferenceDiscoveryTask AssemblyReferenceDiscoveryTask { get; }
    }
}