using System;
using JetBrains.Annotations;

namespace TryRoslyn.Core {
    [ThreadSafe]
    public interface ICodeProcessor : IDisposable {
        [NotNull]
        ProcessingResult Process([NotNull] string code, [CanBeNull] ProcessingOptions options = null);
    }
}