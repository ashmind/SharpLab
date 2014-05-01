using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Core {
    [Serializable]
    public class ProcessingResult {
        public string Decompiled { get; private set; }
        public IList<SerializableDiagnostic> Diagnostics { get; private set; }

        public bool IsSuccess {
            get { return this.Decompiled != null; }
        }

        public ProcessingResult(string decompiled, IEnumerable<SerializableDiagnostic> diagnostics) {
            this.Decompiled = decompiled;
            this.Diagnostics = diagnostics.ToArray(); // can't use immutables here, as they are non-serializable
        }

        public IEnumerable<SerializableDiagnostic> GetDiagnostics(DiagnosticSeverity severity) {
            return this.Diagnostics.Where(d => d.Severity == severity);
        }
    }
}