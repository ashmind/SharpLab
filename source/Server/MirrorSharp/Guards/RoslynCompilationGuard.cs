using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using MirrorSharp.Advanced;
using MirrorSharp.Advanced.EarlyAccess;

namespace SharpLab.Server.MirrorSharp.Guards {
    using Compilation = Microsoft.CodeAnalysis.Compilation;

    public class RoslynCompilationGuard : IRoslynCompilationGuard {
        private readonly IRoslynCompilationGuard<CSharpCompilation> _csharpCompilationGuard;
        private readonly IRoslynCompilationGuard<VisualBasicCompilation> _visualBasicCompilationGuard;

        public RoslynCompilationGuard(
            IRoslynCompilationGuard<CSharpCompilation> csharpCompilationGuard,
            IRoslynCompilationGuard<VisualBasicCompilation> visualBasicCompilationGuard            
        ) {
            _csharpCompilationGuard = csharpCompilationGuard;
            _visualBasicCompilationGuard = visualBasicCompilationGuard;
        }

        public void ValidateCompilation(Compilation compilation, IRoslynSession session) {
            Argument.NotNull(nameof(compilation), compilation);
            switch (compilation) {
                case CSharpCompilation csharp:
                    _csharpCompilationGuard.ValidateCompilation(csharp);
                    break;

                case VisualBasicCompilation visualBasic:
                    _visualBasicCompilationGuard.ValidateCompilation(visualBasic);
                    break;
            }
        }
    }
}
