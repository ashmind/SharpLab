using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TryRoslyn.Core {
    public class ProcessingResult {
        public SyntaxTree SyntaxTree { get; private set; }
        public string Decompiled { get; private set; }
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

        public bool IsSuccess {
            get { return this.Decompiled != null; }
        }

        public ProcessingResult(SyntaxTree syntaxTree, string decompiled, ImmutableArray<Diagnostic> diagnostics) {
            SyntaxTree = syntaxTree;
            this.Decompiled = decompiled;
            this.Diagnostics = diagnostics;
        }

        public IEnumerable<Diagnostic> GetDiagnostics(DiagnosticSeverity severity) {
            return this.Diagnostics.Where(d => d.Severity == severity);
        }
    }
}