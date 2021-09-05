using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Mobius.ILasm.interfaces;
using CodeAnalysis = Microsoft.CodeAnalysis;
using Location = Mono.ILASM.Location;

namespace SharpLab.Server.Compilation.Internal {
    public class ILCompilationLogger : ILogger {
        private ILLineColumnMap? _lineColumnMap;
        private readonly string _text;
        private readonly IList<Diagnostic> _diagnostics;

        public ILCompilationLogger(string text, IList<Diagnostic> diagnostics) {
            _text = text;
            _diagnostics = diagnostics;
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
            _lineColumnMap ??= ILLineColumnMap.BuildFor(_text);

            var offset = _lineColumnMap.GetOffset(location.line, location.column);
            var length = Math.Min(2, _text.Length - offset);

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
