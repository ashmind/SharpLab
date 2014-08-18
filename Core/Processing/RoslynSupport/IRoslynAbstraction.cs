using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    [ThreadSafe]
    public interface IRoslynAbstraction {
        [Pure]
        SyntaxTree ParseText<TParseOptions>(Type syntaxTreeType, string code, TParseOptions options)
            where TParseOptions : ParseOptions;

        [Pure]
        EmitResult Emit(Compilation compilation, Stream stream);

        [Pure]
        MetadataReference MetadataReferenceFromPath(string path);

        [Pure]
        TParseOptions NewParseOptions<TLanguageVersion, TParseOptions>(TLanguageVersion languageVersion, SourceCodeKind kind);

        [Pure]
        TCompilationOptions NewCompilationOptions<TCompilationOptions>(OutputKind outputKind);

        [Pure]
        TCompilationOptions WithOptimizationLevel<TCompilationOptions>(TCompilationOptions options, OptimizationLevelAbstraction value);

        [Pure]
        TLanguageVersion GetMaxValue<TLanguageVersion>();
    }
}
