using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TryRoslyn.Core.Processing.RoslynSupport {
    public interface IRoslynAbstraction {
        MetadataFileReference NewMetadataFileReference(string path);
        CSharpCompilationOptions NewCSharpCompilationOptions(OutputKind outputKind);
    }
}
