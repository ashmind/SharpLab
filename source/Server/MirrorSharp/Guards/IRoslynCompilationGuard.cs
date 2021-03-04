namespace SharpLab.Server.MirrorSharp.Guards {
    using Compilation = Microsoft.CodeAnalysis.Compilation;

    public interface IRoslynCompilationGuard<TCompilation>
        where TCompilation : Compilation
    {
        void ValidateCompilation(TCompilation compilation);
    }
}
