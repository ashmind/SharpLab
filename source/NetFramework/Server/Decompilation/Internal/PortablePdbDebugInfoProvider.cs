using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler.DebugInfo;
using Decompiler = ICSharpCode.Decompiler;

namespace SharpLab.Server.Decompilation.Internal {
    internal class PortablePdbDebugInfoProvider : IDisposableDebugInfoProvider {
        private readonly MetadataReaderProvider _readerProvider;
        private readonly MetadataReader _reader;

        public PortablePdbDebugInfoProvider(Stream symbolStream) {
            _readerProvider = MetadataReaderProvider.FromPortablePdbStream(symbolStream);
            _reader = _readerProvider.GetMetadataReader();
        }

        public string SourceFileName => "_";
        public string Description => "";

        public IList<Decompiler.DebugInfo.SequencePoint> GetSequencePoints(MethodDefinitionHandle method) {
            var debugInfo = _reader.GetMethodDebugInformation(method);

            var points = debugInfo.GetSequencePoints();
            var result = new List<Decompiler.DebugInfo.SequencePoint>();
            foreach (var point in points) {
                result.Add(new Decompiler.DebugInfo.SequencePoint {
                    Offset = point.Offset,
                    StartLine = point.StartLine,
                    StartColumn = point.StartColumn,
                    EndLine = point.EndLine,
                    EndColumn = point.EndColumn,
                    DocumentUrl = "_"
                });
            }
            return result;
        }

        public IList<Variable> GetVariables(MethodDefinitionHandle method) {
            var variables = new List<Variable>();
            foreach (var local in EnumerateLocals(method)) {
                variables.Add(new Variable(local.Index, _reader.GetString(local.Name)));
            }
            return variables;
        }

        public bool TryGetName(MethodDefinitionHandle method, int index, out string? name) {
            name = null;
            foreach (var local in EnumerateLocals(method)) {
                if (local.Index == index) {
                    name = _reader.GetString(local.Name);
                    return true;
                }
            }
            return false;
        }

        private IEnumerable<LocalVariable> EnumerateLocals(MethodDefinitionHandle method) {
            foreach (var scopeHandle in _reader.GetLocalScopes(method)) {
                var scope = _reader.GetLocalScope(scopeHandle);
                foreach (var variableHandle in scope.GetLocalVariables()) {
                    yield return _reader.GetLocalVariable(variableHandle);
                }
            }
        }

        public void Dispose() {
            _readerProvider.Dispose();
        }
    }
}