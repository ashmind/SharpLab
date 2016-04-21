using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    [ThreadSafe, Obsolete]
    public interface IRoslynAbstraction {
        [Pure, Obsolete]
        SyntaxTree ParseText<TParseOptions>(Type syntaxTreeType, string code, TParseOptions options)
            where TParseOptions : ParseOptions;

        [Pure, Obsolete]
        EmitResult Emit(Compilation compilation, Stream stream);

        [Pure, Obsolete]
        TCompilationOptions NewCompilationOptions<TCompilationOptions>(OutputKind outputKind);

        [Pure, Obsolete]
        TCompilationOptions WithOptimizationLevel<TCompilationOptions>(TCompilationOptions options, OptimizationLevelAbstraction value);
    }
}
