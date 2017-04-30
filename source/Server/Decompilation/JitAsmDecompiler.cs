using System;
using AshMind.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AppDomainToolkit;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Runtime;
using SharpDisasm;
using SharpDisasm.Translators;
using TryRoslyn.Server.Decompilation.Support;

namespace TryRoslyn.Server.Decompilation {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class JitAsmDecompiler : IDecompiler {
        public string LanguageName => "JIT ASM";

        public void Decompile(Stream assemblyStream, TextWriter codeWriter) {
            var currentSetup = AppDomain.CurrentDomain.SetupInformation;
            using (var dataTarget = DataTarget.AttachToProcess(CurrentProcess.Id, UInt32.MaxValue, AttachFlag.Passive))
            using (var context = AppDomainContext.Create(new AppDomainSetup {
                ApplicationBase = currentSetup.ApplicationBase,
                PrivateBinPath = currentSetup.PrivateBinPath
            })) {
                context.LoadAssembly(LoadMethod.LoadFrom, Assembly.GetExecutingAssembly().GetAssemblyFile().FullName);
                var results = RemoteFunc.Invoke(context.Domain, assemblyStream, Remote.GetCompiledMethods);

                var currentMethodAddressRef = new Reference<ulong>();
                var runtime = dataTarget.ClrVersions.Single().CreateRuntime();
                var translator = new IntelTranslator {
                    SymbolResolver = (Instruction instruction, long addr, ref long offset) => 
                        ResolveSymbol(runtime, instruction, addr, currentMethodAddressRef.Value)
                };

                codeWriter.WriteLine("; This is an experimental implementation.");
                codeWriter.WriteLine("; Please report any bugs to https://github.com/ashmind/TryRoslyn/issues.");
                codeWriter.WriteLine();
                foreach (var result in results) {
                    var methodHandle = (ulong)result.Handle.ToInt64();
                    var method = runtime.GetMethodByHandle(methodHandle);
                    if (method == null) {
                        codeWriter.WriteLine("    ; Method with handle 0x{0:X} was somehow not found by CLRMD.", methodHandle);
                        codeWriter.WriteLine("    ; See https://github.com/ashmind/TryRoslyn/issues/84.");
                        continue;
                    }

                    DisassembleAndWrite(method, result.Message, translator, currentMethodAddressRef, codeWriter);
                    codeWriter.WriteLine();
                }
            }
        }

        private static string ResolveSymbol(ClrRuntime runtime, Instruction instruction, long addr, ulong currentMethodAddress) {
            var operand = instruction.Operands.Length > 0 ? instruction.Operands[0] : null;
            if (operand?.PtrOffset == 0) {
                var baseOffset = instruction.PC - currentMethodAddress;
                return $"L{baseOffset + operand.PtrSegment:x4}";
            }

            return runtime.GetMethodByAddress(unchecked((ulong)addr))?.GetFullSignature();
        }

        private void DisassembleAndWrite(ClrMethod method, string message, Translator translator, Reference<ulong> methodAddressRef, TextWriter writer) {
            writer.WriteLine(method.GetFullSignature());
            if (message != null) {
                writer.Write("    ; ");
                writer.WriteLine(message);
                return;
            }

            var methodAddress = method.HotColdInfo.HotStart;
            if (methodAddress == 0) {
                writer.WriteLine("    ; Method HotStart is 0, not sure why yet.");
                writer.WriteLine("    ; See https://github.com/ashmind/TryRoslyn/issues/82.");
                return;
            }

            var hotSize = method.HotColdInfo.HotSize;
            if (hotSize == 0) {
                writer.WriteLine("    ; Method HotSize is 0, not sure why yet.");
                writer.WriteLine("    ; See https://github.com/ashmind/TryRoslyn/issues/82.");
                return;
            }

            methodAddressRef.Value = methodAddress;
            using (var disasm = new Disassembler(new IntPtr(unchecked((long)methodAddress)), (int)hotSize, ArchitectureMode.x86_64, methodAddress)) {
                foreach (var instruction in disasm.Disassemble()) {
                    writer.Write("    L{0:x4}: ", instruction.Offset - methodAddress);
                    writer.WriteLine(translator.Translate(instruction));
                }
            }
        }

        private class Reference<T> {
            public T Value { get; set; }
        }

        private static class Remote {
            public static IReadOnlyList<MethodJitResult> GetCompiledMethods(Stream assemblyStream) {
                var assembly = Assembly.Load(ReadAllBytes(assemblyStream));
                var results = new List<MethodJitResult>();
                foreach (var type in assembly.DefinedTypes) {
                    CompileAndCollectMembers(results, type);
                }
                return results;
            }

            private static void CompileAndCollectMembers(ICollection<MethodJitResult> results, TypeInfo type) {
                foreach (var constructor in type.DeclaredConstructors) {
                    results.Add(CompileAndWrap(constructor));
                }

                foreach (var method in type.DeclaredMethods) {
                    if (method.IsAbstract)
                        continue;
                    results.Add(CompileAndWrap(method));
                }
            }

            private static MethodJitResult CompileAndWrap(MethodBase method) {
                var handle = method.MethodHandle;
                if (method.IsGenericMethodDefinition || (method.DeclaringType?.IsGenericTypeDefinition ?? false))
                    return new MethodJitResult(handle.Value, "Open generics cannot be JIT-compiled.");

                RuntimeHelpers.PrepareMethod(handle);
                return new MethodJitResult(handle.Value);
            }

            private static byte[] ReadAllBytes(Stream stream) {
                byte[] bytes;
                if (stream is MemoryStream memoryStream) {
                    bytes = memoryStream.GetBuffer();
                    if (bytes.Length != memoryStream.Length)
                        bytes = memoryStream.ToArray();
                    return bytes;
                }

                // we can't use ArrayPool here as this method is called in a temp AppDomain
                bytes = new byte[stream.Length];
                if (stream.Read(bytes, 0, (int)stream.Length) != bytes.Length)
                    throw new NotSupportedException();

                return bytes;
            }

            [Serializable]
            public struct MethodJitResult {
                public MethodJitResult(IntPtr handle, string message = null) {
                    Handle = handle;
                    Message = message;
                }
                
                public IntPtr Handle { get; }
                public string Message { get; }
            }
        }
    }
}