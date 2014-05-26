using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    [ThreadSafe]
    public interface IRoslynAbstraction {
        SyntaxTree ParseText<TSyntaxTree>(string code, ParseOptions options);
        MetadataFileReference NewMetadataFileReference(string path);
        TCompilationOptions NewCompilationOptions<TCompilationOptions>(OutputKind outputKind);
        TLanguageVersion GetMaxValue<TLanguageVersion>();
    }
}
