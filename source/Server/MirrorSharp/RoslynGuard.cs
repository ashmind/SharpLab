using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using MirrorSharp.Advanced;
using MirrorSharp.Advanced.EarlyAccess;
using SharpLab.Server.Compilation.Guards;

namespace SharpLab.Server.MirrorSharp {
    using Compilation = Microsoft.CodeAnalysis.Compilation;

    public class RoslynGuard : IRoslynGuard {
        private readonly IRoslynGuardInternal<CSharpCompilation> _csharpGuard;
        private readonly IRoslynGuardInternal<VisualBasicCompilation> _visualBasicGuard;

        public RoslynGuard(
            IRoslynGuardInternal<CSharpCompilation> csharpGuard,
            IRoslynGuardInternal<VisualBasicCompilation> visualBasicGuard
        ) {
            _csharpGuard = csharpGuard;
            _visualBasicGuard = visualBasicGuard;
        }

        public void ValidateCompilation(Compilation compilation, IRoslynSession session) {
            Argument.NotNull(nameof(compilation), compilation);
            switch (compilation) {
                case CSharpCompilation csharp:
                    _csharpGuard.ValidateCompilation(csharp);
                    break;

                case VisualBasicCompilation visualBasic:
                    _visualBasicGuard.ValidateCompilation(visualBasic);
                    break;
            }
        }
    }
}
