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
        EmitResult Emit(Compilation compilation, Stream stream);
    }
}
