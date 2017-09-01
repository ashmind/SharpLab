using JetBrains.Annotations;
using MirrorSharp;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Common {
    public interface ILanguageAdapter {
        [NotNull] string LanguageName { get; }

        void SlowSetup([NotNull] MirrorSharpOptions options);
        void SetOptimize([NotNull] IWorkSession session, [NotNull] string optimize);
        void SetOptionsForTarget([NotNull] IWorkSession session, [NotNull] string target);

        [CanBeNull] int? GetMethodStartLine(IWorkSession session, int lineInMethod, int columnInMethod);
    }
}