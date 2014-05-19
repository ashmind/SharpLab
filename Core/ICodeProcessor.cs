using System;
using JetBrains.Annotations;

namespace TryRoslyn.Core {
    [ThreadSafe]
    public interface ICodeProcessor : IDisposable {
        ProcessingResult Process(string code, bool scriptMode = false);
    }
}