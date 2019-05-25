using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Runtime;
using SharpDisasm;
using SharpDisasm.Translators;
using SharpLab.Server.Common;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public abstract class JitAsmDecompilerBase : IDecompiler {
        public string LanguageName => TargetNames.JitAsm;

        public void Decompile(CompilationStreamPair streams, TextWriter codeWriter) {
            Argument.NotNull(nameof(streams), streams);
            Argument.NotNull(nameof(codeWriter), codeWriter);

            using (var resultScope = JitCompileAndGetMethods(streams.AssemblyStream)) 
            using (var dataTarget = DataTarget.AttachToProcess(Current.ProcessId, uint.MaxValue, AttachFlag.Passive)) {
                var currentMethodAddressRef = new Reference<ulong>();
                var runtime = dataTarget.ClrVersions.Single(v => v.Flavor == ClrFlavor).CreateRuntime();
                var translator = new IntelTranslator {
                    SymbolResolver = (Instruction instruction, long addr, ref long offset) =>
                        ResolveSymbol(runtime, instruction, addr, currentMethodAddressRef.Value)
                };

                WriteJitInfo(runtime.ClrInfo, codeWriter);
                WriteProfilerState(codeWriter);
                codeWriter.WriteLine();

                var architecture = MapArchitecture(runtime.ClrInfo.DacInfo.TargetArchitecture);
                foreach (var result in resultScope.Results) {
                    DisassembleAndWrite(result, runtime, architecture, translator, currentMethodAddressRef, codeWriter);
                    codeWriter.WriteLine();
                }
            }
        }

        protected abstract ClrFlavor ClrFlavor { get; }
        protected abstract JitAsmResultScope JitCompileAndGetMethods(MemoryStream assemblyStream);

        private void WriteJitInfo(ClrInfo clr, TextWriter writer) {
            writer.WriteLine(
                "; {0:G} CLR {1} ({2}) on {3}.",
                clr.Flavor, clr.Version, Path.GetFileName(clr.ModuleInfo.FileName), clr.DacInfo.TargetArchitecture.ToString("G").ToLowerInvariant()
            );
        }

        private void WriteProfilerState(TextWriter writer) {
            if (!ProfilerState.Active)
                return;

            writer.WriteLine("; Note: Running under profiler, which affects JIT assembly in heap allocations.");
        }

        private static string ResolveSymbol(ClrRuntime runtime, Instruction instruction, long addr, ulong currentMethodAddress) {
            var operand = instruction.Operands.Length > 0 ? instruction.Operands[0] : null;
            if (operand?.PtrOffset == 0) {
                var lvalue = GetOperandLValue(operand!);
                if (lvalue == null)
                    return $"{operand!.RawValue} ; failed to resolve lval ({operand.Size}), please report at https://github.com/ashmind/SharpLab/issues";
                var baseOffset = instruction.PC - currentMethodAddress;
                return $"L{baseOffset + lvalue:x4}";
            }

            return runtime.GetMethodByAddress(unchecked((ulong)addr))?.GetFullSignature();
        }

        private static ulong? GetOperandLValue(Operand operand) {
            switch (operand.Size) {
                case 8:  return (ulong)operand.LvalSByte;
                case 16: return (ulong)operand.LvalSWord;
                case 32: return (ulong)operand.LvalSDWord;
                default: return null;
            }
        }

        private void DisassembleAndWrite(MethodJitResult result, ClrRuntime runtime, ArchitectureMode architecture, Translator translator, Reference<ulong> methodAddressRef, TextWriter writer) {
            var (method, regions) = ResolveJitResult(runtime, result);
            if (method == null) {
                writer.WriteLine("Unknown (0x{0:X})", (ulong)result.Handle.ToInt64());
                writer.WriteLine("    ; Method was not found by CLRMD (reason unknown).");
                writer.WriteLine("    ; See https://github.com/ashmind/SharpLab/issues/84.");
                return;
            }

            writer.WriteLine(method.GetFullSignature());
            switch (result.Status) {
                case MethodJitStatus.IgnoredRuntime:
                    writer.WriteLine("    ; Cannot produce JIT assembly for runtime-implemented method.");
                    return;
                case MethodJitStatus.IgnoredOpenGenericWithNoAttribute:
                    writer.WriteLine("    ; Open generics cannot be JIT-compiled.");
                    writer.WriteLine("    ; However you can use attribute SharpLab.Runtime.JitGeneric to specify argument types.");
                    writer.WriteLine("    ; Example: [JitGeneric(typeof(int)), JitGeneric(typeof(string))] void M<T>() { ... }.");
                    return;
            }

            if (regions == null) {
                if (result.Status == MethodJitStatus.SuccessGeneric) {
                    writer.WriteLine("    ; Failed to find HotColdInfo for generic method (reference types?).");
                    writer.WriteLine("    ; If you know a solution, please comment at https://github.com/ashmind/SharpLab/issues/99.");
                    return;
                }
                writer.WriteLine("    ; Failed to find HotColdRegions — please report at https://github.com/ashmind/SharpLab/issues.");
                return;
            }

            var methodAddress = regions.HotStart;
            methodAddressRef.Value = methodAddress;
            using (var disasm = new Disassembler(new IntPtr(unchecked((long)methodAddress)), (int)regions.HotSize, architecture, methodAddress)) {
                foreach (var instruction in disasm.Disassemble()) {
                    writer.Write("    L");
                    writer.Write((instruction.Offset - methodAddress).ToString("x4"));
                    writer.Write(": ");
                    writer.WriteLine(translator.Translate(instruction));
                }
            }
        }

        private (ClrMethod? method, HotColdRegions? regions) ResolveJitResult(ClrRuntime runtime, MethodJitResult result) {
            ClrMethod? methodByPointer = null;
            if (result.Pointer != null) {
                methodByPointer = runtime.GetMethodByAddress((ulong)result.Pointer.Value.ToInt64());
                if (methodByPointer != null) {
                    if (!result.IsSuccess)
                        return (methodByPointer, null);

                    var regionsByPointer = FindNonEmptyHotColdInfo(methodByPointer);
                    if (regionsByPointer != null)
                        return (methodByPointer, regionsByPointer);
                }
            }

            var methodByHandle = runtime.GetMethodByHandle((ulong)result.Handle.ToInt64());
            if (methodByHandle == null)
                return (methodByPointer, null);
            if (!result.IsSuccess)
                return (methodByHandle, null);
            var regionsByHandle = FindNonEmptyHotColdInfo(methodByHandle);
            if (regionsByHandle == null && methodByPointer != null)
                return (methodByPointer, null);

            return (methodByHandle, regionsByHandle);
        }

        private HotColdRegions? FindNonEmptyHotColdInfo(ClrMethod method) {
            // I can't really explain this, but it seems that some methods 
            // are present multiple times in the same type -- one compiled
            // and one not compiled. A bug in clrmd?
            if (method.HotColdInfo.HotSize > 0)
                return method.HotColdInfo;

            if (method.Type == null)
                return null;

            var methodSignature = method.GetFullSignature();
            foreach (var other in method.Type.Methods) {
                if (other.MetadataToken == method.MetadataToken && other.GetFullSignature() == methodSignature && other.HotColdInfo.HotSize > 0)
                    return other.HotColdInfo;
            }

            return null;
        }

        private ArchitectureMode MapArchitecture(Architecture architecture) {
            switch (architecture) {
                case Architecture.Amd64: return ArchitectureMode.x86_64;
                case Architecture.X86: return ArchitectureMode.x86_32;
                // ReSharper disable once HeapView.BoxingAllocation
                // ReSharper disable once HeapView.ObjectAllocation.Evident
                default: throw new Exception($"Unsupported architecture mode {architecture}.");
            }
        }

        #pragma warning disable CS8618 // Non-nullable field is uninitialized.
        private class Reference<T> {
        #pragma warning restore CS8618 // Non-nullable field is uninitialized.
            public T Value { get; set; }
        }

        private static class Remote {
            public static IReadOnlyList<MethodJitResult> GetCompiledMethods(byte[] assemblyBytes) {
                var assembly = Assembly.Load(assemblyBytes);
                return IsolatedJitAsmDecompilerCore.JitCompileAndGetMethods(assembly);
            }
        }
    }
}