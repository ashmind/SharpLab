using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Mobius.ILasm.interfaces;
using CodeAnalysis = Microsoft.CodeAnalysis;
using Location = Mono.ILASM.Location;

namespace SharpLab.Server.Compilation.Internal {
    public class ILCompilationLogger : ILogger {
        private readonly IList<Diagnostic> _diagnostics;
        private readonly ILLineColumnMap _lineColumnMap;

        public ILCompilationLogger(IList<Diagnostic> diagnostics, ILLineColumnMap lineColumnMap) {
            _diagnostics = diagnostics;
            _lineColumnMap = lineColumnMap;
        }

        public void Info(string message) {
        }

        public void Error(string message) {
            AddDiagnostic(DiagnosticSeverity.Error, message);
        }

        public void Error(Location location, string message) {
            AddDiagnostic(DiagnosticSeverity.Error, message, location);
        }

        public void Warning(string message) {
            AddDiagnostic(DiagnosticSeverity.Warning, message, warningLevel: 1);
        }

        public void Warning(Location location, string message) {
            AddDiagnostic(DiagnosticSeverity.Warning, message, location, warningLevel: 1);
        }

        private void AddDiagnostic(
            DiagnosticSeverity severity,
            string message,
            Location? location = null,
            int warningLevel = 0
        ) {
            _diagnostics.Add(Diagnostic.Create(
                "IL", "Compiler", message,
                severity,
                severity,
                isEnabledByDefault: true,
                warningLevel,
                location: location != null ? ConvertLocation(location) : null
            ));
        }

        private CodeAnalysis.Location ConvertLocation(Location location) {
            var offset = _lineColumnMap.GetOffset(location.line, location.column);
            if (offset == _lineColumnMap.TextLength)
                offset = _lineColumnMap.TextLength - 2;
            var length = Math.Min(2, _lineColumnMap.TextLength - offset);

            return CodeAnalysis.Location.Create(
                "_",
                new TextSpan(offset, length),
                new LinePositionSpan(
                    new LinePosition(location.line, location.column),
                    new LinePosition(location.line, location.column + length)
                )
            );
        }
    }
}
