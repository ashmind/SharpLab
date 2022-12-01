using System;
using System.IO;
using System.Reflection.Metadata;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    public class ILDecompiler : IILDecompiler {
        private readonly Func<Stream, IDisposableDebugInfoProvider> _debugInfoFactory;
        private readonly MemoryPoolSlim<TypeDefinitionHandle> _typeHandleMemoryPool;

        public ILDecompiler(
            Func<Stream, IDisposableDebugInfoProvider> debugInfoFactory,
            MemoryPoolSlim<TypeDefinitionHandle> typeHandleMemoryPool
        ) {
            _debugInfoFactory = debugInfoFactory;
            _typeHandleMemoryPool = typeHandleMemoryPool;
        }

        public void Decompile(CompilationStreamPair streams, TextWriter codeWriter, IWorkSession session) {
            Argument.NotNull(nameof(streams), streams);
            Argument.NotNull(nameof(codeWriter), codeWriter);
            Argument.NotNull(nameof(session), session);

            Decompile(streams.AssemblyStream, streams.SymbolStream, codeWriter);
        }

        public void Decompile(Stream assemblyStream, Stream? symbolStream, TextWriter codeWriter) {
            Argument.NotNull(nameof(assemblyStream), assemblyStream);
            Argument.NotNull(nameof(codeWriter), codeWriter);

            using var assemblyFile = new PEFile("_", assemblyStream);
            using var debugInfo = symbolStream != null ? _debugInfoFactory(symbolStream) : null;

            var output = new PlainTextOutput(codeWriter) { IndentationString = "    " };
            var disassembler = new ReflectionDisassembler(output, CancellationToken.None) {
                DebugInfo = debugInfo,
                ShowSequencePoints = true
            };

            disassembler.WriteAssemblyHeader(assemblyFile);
            output.WriteLine(); // empty line

            var metadata = assemblyFile.Metadata;
            DecompileTypes(assemblyFile, output, disassembler, metadata);
        }

        private void DecompileTypes(PEFile assemblyFile, PlainTextOutput output, ReflectionDisassembler disassembler, MetadataReader metadata) {
            const int MaxNonUserTypeHandles = 10;
            var nonUserTypeHandlesLease = default(MemoryLease<TypeDefinitionHandle>);
            var nonUserTypeHandlesCount = -1;

            try {
                // user code (first)
                foreach (var typeHandle in metadata.TypeDefinitions) {
                    var type = metadata.GetTypeDefinition(typeHandle);
                    if (!type.GetDeclaringType().IsNil)
                        continue; // not a top-level type

                    if (IsNonUserCode(metadata, type) && nonUserTypeHandlesCount < MaxNonUserTypeHandles) {
                        if (nonUserTypeHandlesCount == -1) {
                            nonUserTypeHandlesLease = _typeHandleMemoryPool.RentExact(25);
                            nonUserTypeHandlesCount = 0;
                        }

                        nonUserTypeHandlesLease.AsSpan()[nonUserTypeHandlesCount] = typeHandle;
                        nonUserTypeHandlesCount += 1;
                        continue;
                    }

                    disassembler.DisassembleType(assemblyFile, typeHandle);
                    output.WriteLine();
                }

                // non-user code (second)
                if (nonUserTypeHandlesCount > 0) {
                    foreach (var typeHandle in nonUserTypeHandlesLease.AsSpan().Slice(0, nonUserTypeHandlesCount)) {
                        disassembler.DisassembleType(assemblyFile, typeHandle);
                        output.WriteLine();
                    }
                }
            }
            finally {
                nonUserTypeHandlesLease.Dispose();
            }
        }

        private bool IsNonUserCode(MetadataReader metadata, TypeDefinition type) {
            return type.IsCompilerGenerated(metadata)
                && !type.NamespaceDefinition.IsNil;
        }

        public string LanguageName => TargetNames.IL;
    }
}