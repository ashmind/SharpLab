using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Core {
    [Serializable]
    public class ProcessingResult {
        public string Decompiled { get; private set; }
        public IReadOnlyList<ProcessingResultDiagnostic> Diagnostics { get; private set; }

        public bool IsSuccess {
            get { return Decompiled != null; }
        }

        public ProcessingResult(string decompiled, IEnumerable<ProcessingResultDiagnostic> diagnostics) {
            Decompiled = decompiled;
            Diagnostics = diagnostics.ToArray(); // can't use immutables here, as they are non-serializable
        }

        public IEnumerable<ProcessingResultDiagnostic> GetDiagnostics(DiagnosticSeverity severity) {
            return Diagnostics.Where(d => d.Severity == severity);
        }
    }
}