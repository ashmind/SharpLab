using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    [ThreadSafe]
    public interface IRoslynAbstraction {
        [Pure]
        SyntaxTree ParseText<TParseOptions>(Type syntaxTreeType, string code, TParseOptions options)
            where TParseOptions : ParseOptions;

        [Pure]
        MetadataFileReference NewMetadataFileReference(string path);

        [Pure]
        TCompilationOptions NewCompilationOptions<TCompilationOptions>(OutputKind outputKind);

        [Pure]
        TLanguageVersion GetMaxValue<TLanguageVersion>();
    }
}
