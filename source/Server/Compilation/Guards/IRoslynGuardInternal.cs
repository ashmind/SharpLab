namespace SharpLab.Server.Compilation.Guards {
    using Compilation = Microsoft.CodeAnalysis.Compilation;

    public interface IRoslynGuardInternal<TCompilation>
        where TCompilation : Compilation
    {
        void ValidateCompilation(TCompilation compilation);
    }
}
