using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    [ThreadSafe]
    public interface IRoslynAbstraction {
        MetadataFileReference NewMetadataFileReference(string path);
        CSharpCompilationOptions NewCSharpCompilationOptions(OutputKind outputKind);
        LanguageVersion GetMaxLanguageVersion();
    }
}
