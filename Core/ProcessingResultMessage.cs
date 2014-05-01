using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Core {
    [Serializable]
    public class SerializableDiagnostic {
        private readonly string _string;

        public SerializableDiagnostic(Diagnostic diagnostic) {
            _string = diagnostic.ToString();
            Severity = diagnostic.Severity;
        }

        public DiagnosticSeverity Severity { get; private set; }

        public override string ToString() {
            return _string;
        }
    }
}
