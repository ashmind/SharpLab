using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.DacInterface;
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

            using var resultScope = JitCompileAndGetMethods(streams.AssemblyStream);
            using var dataTarget = DataTarget.AttachToProcess(Current.ProcessId, suspend: false);

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

        protected abstract ClrFlavor ClrFlavor { get; }
        protected abstract JitAsmResultScope JitCompileAndGetMethods(MemoryStream assemblyStream);

        private void WriteJitInfo(ClrInfo clr, TextWriter writer) {
            writer.WriteLine(
                "; {0:G} CLR {1} on {2}",
                clr.Flavor, clr.Version, clr.DacInfo.TargetArchitecture.ToString("G").ToLowerInvariant()
            );
        }

        private void WriteProfilerState(TextWriter writer) {
            if (!ProfilerState.Active)
                return;

            writer.WriteLine("; Note: Running under profiler, which affects JIT assembly in heap allocations.");
        }

        private static string? ResolveSymbol(ClrRuntime runtime, Instruction instruction, long addr, ulong currentMethodAddress) {
            var operand = instruction.Operands.Length > 0 ? instruction.Operands[0] : null;
            if (operand?.PtrOffset == 0) {
                var lvalue = GetOperandLValue(operand!);
                if (lvalue == null)
                    return $"{operand!.RawValue} ; failed to resolve lval ({operand.Size}), please report at https://github.com/ashmind/SharpLab/issues";
                var baseOffset = instruction.PC - currentMethodAddress;
                return $"L{baseOffset + lvalue:x4}";
            }

            return runtime.GetMethodByInstructionPointer(unchecked((ulong)addr))?.Signature;
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
            void WriteSignatureFromClrMethod() {
                var signature = runtime.GetMethodByHandle(unchecked((ulong)result.Handle.ToInt64()))?.Signature;
                WriteSignature(signature);
            }

            void WriteSignature(string? signature) {
                if (signature != null) {
                    writer.WriteLine(signature);
                }
                else {
                    writer.WriteLine("Unknown (0x{0:X})", (ulong)result.Handle.ToInt64());
                    writer.WriteLine("    ; Method signature was not found -- please report this issue.");
                }
            }

            switch (result.Status) {
                case MethodJitStatus.IgnoredRuntime:
                    WriteSignatureFromClrMethod();
                    writer.WriteLine("    ; Cannot produce JIT assembly for runtime-implemented method.");
                    return;
                case MethodJitStatus.IgnoredOpenGenericWithNoAttribute:
                    WriteSignatureFromClrMethod();
                    writer.WriteLine("    ; Open generics cannot be JIT-compiled.");
                    writer.WriteLine("    ; However you can use attribute SharpLab.Runtime.JitGeneric to specify argument types.");
                    writer.WriteLine("    ; Example: [JitGeneric(typeof(int)), JitGeneric(typeof(string))] void M<T>() { ... }.");
                    return;
            }

            if (FindJitCompiledMethod(runtime, result) is not {} method) {
                WriteSignatureFromClrMethod();
                if (result.Status == MethodJitStatus.SuccessGeneric) {
                    writer.WriteLine("    ; Failed to find JIT output for generic method (reference types?).");
                    writer.WriteLine("    ; If you know a solution, please comment at https://github.com/ashmind/SharpLab/issues/99.");
                    return;
                }

                writer.WriteLine("    ; Failed to find JIT output — please report at https://github.com/ashmind/SharpLab/issues.");
                return;
            }

            WriteSignature(method.Signature);
            var methodAddress = method.MethodAddress;
            methodAddressRef.Value = methodAddress;
            using (var disasm = new Disassembler(new IntPtr(unchecked((long)methodAddress)), (int)method.MethodSize, architecture, methodAddress)) {
                foreach (var instruction in disasm.Disassemble()) {
                    writer.Write("    L");
                    writer.Write((instruction.Offset - methodAddress).ToString("x4"));
                    writer.Write(": ");
                    writer.WriteLine(translator.Translate(instruction));
                }
            }
        }

        private ClrMethodData? FindJitCompiledMethod(ClrRuntime runtime, MethodJitResult result) {
            var sos = runtime.DacLibrary.SOSDacInterface;

            var methodDescAddress = unchecked((ulong)result.Handle.ToInt64());
            if (!sos.GetMethodDescData(methodDescAddress, 0, out var methodDesc))
                return null;

            return GetJitCompiledMethodByMethodDescIfValid(sos, methodDesc)
                ?? FindJitCompiledMethodInMethodTable(sos, methodDesc);
        }

        private ClrMethodData? GetJitCompiledMethodByMethodDescIfValid(SOSDac sos, MethodDescData methodDesc) {
            // https://github.com/microsoft/clrmd/issues/935
            var codeHeaderAddress = methodDesc.HasNativeCode != 0
                ? (ulong)methodDesc.NativeCodeAddr
                : sos.GetMethodTableSlot(methodDesc.MethodTable, methodDesc.SlotNumber);

            if (codeHeaderAddress == unchecked((ulong)-1))
                return null;

            if (!sos.GetCodeHeaderData(codeHeaderAddress, out var codeHeader))
                return null;

            return GetJitCompiledMethodByCodeHeaderIfValid(sos, codeHeader);
        }

        private ClrMethodData? GetJitCompiledMethodByCodeHeaderIfValid(SOSDac sos, CodeHeaderData codeHeader) {
            if (codeHeader.MethodStart.Value == -1 || codeHeader.HotRegionSize == 0)
                return null;

            return new(
                sos.GetMethodDescName(codeHeader.MethodDesc),
                unchecked((ulong)codeHeader.MethodStart.Value),
                codeHeader.HotRegionSize
            );
        }

        private ClrMethodData? FindJitCompiledMethodInMethodTable(SOSDac sos, MethodDescData originalMethodDesc) {
            // I can't really explain this, but it seems that some methods 
            // are present multiple times in the same type -- one compiled
            // and one not compiled.

            if (!sos.GetMethodTableData(originalMethodDesc.MethodTable, out var methodTable))
                return null;

            ClrMethodData? methodData = null;
            for (var i = 0u; i < methodTable.NumMethods; i++) {
                if (i == originalMethodDesc.SlotNumber)
                    continue;

                var slot = sos.GetMethodTableSlot(originalMethodDesc.MethodTable, i);
                if (!sos.GetCodeHeaderData(slot, out var candidateCodeHeader))
                    continue;

                if (!sos.GetMethodDescData(candidateCodeHeader.MethodDesc, 0, out var candidateMethodDesc))
                    continue;

                if (candidateMethodDesc.MDToken != originalMethodDesc.MDToken)
                    continue;

                methodData = GetJitCompiledMethodByCodeHeaderIfValid(sos, candidateCodeHeader);
                if (methodData != null)
                    break;
            }
            return methodData;
        }

        private ArchitectureMode MapArchitecture(Architecture architecture) => architecture switch {
            Architecture.Amd64 => ArchitectureMode.x86_64,
            Architecture.X86 => ArchitectureMode.x86_32,
            // ReSharper disable once HeapView.BoxingAllocation
            // ReSharper disable once HeapView.ObjectAllocation.Evident
            _ => throw new Exception($"Unsupported architecture mode {architecture}."),
        };

        private class Reference<T> {
            #pragma warning disable CS8618 // Non-nullable field is uninitialized.
            public T Value { get; set; }
            #pragma warning restore CS8618 // Non-nullable field is uninitialized.
        }

        private static class Remote {
            public static IReadOnlyList<MethodJitResult> GetCompiledMethods(byte[] assemblyBytes) {
                var assembly = Assembly.Load(assemblyBytes);
                return IsolatedJitAsmDecompilerCore.JitCompileAndGetMethods(assembly);
            }
        }

        private readonly struct ClrMethodData {
            public ClrMethodData(string? signature, ulong methodAddress, uint methodSize) {
                Signature = signature;
                MethodAddress = methodAddress;
                MethodSize = methodSize;
            }

            public string? Signature { get; }
            public ulong MethodAddress { get; }
            public uint MethodSize { get; }
        }
    }
}