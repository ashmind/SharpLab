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
                
                WriteJitInfo(runtime.ClrInfo, codeWriter);

                var architecture = MapArchitecture(runtime.ClrInfo.DacInfo.TargetArchitecture);
                foreach (var result in results) {
                    var methodHandle = (ulong)result.Handle.ToInt64();
                    var method = runtime.GetMethodByHandle(methodHandle);
                    if (method == null) {
                        codeWriter.WriteLine("    ; Method with handle 0x{0:X} was somehow not found by CLRMD.", methodHandle);
                        codeWriter.WriteLine("    ; See https://github.com/ashmind/TryRoslyn/issues/84.");
                        continue;
                    }

                    DisassembleAndWrite(method, result.Message, architecture, translator, currentMethodAddressRef, codeWriter);
                    codeWriter.WriteLine();
                }
            }
        }

        private void WriteJitInfo(ClrInfo clr, TextWriter writer) {
            writer.Write("; ");
            // ReSharper disable once HeapView.BoxingAllocation (is it worth caching?)
            writer.Write(clr.Flavor.ToString("G"));
            writer.Write(" CLR ");
            writer.Write(clr.Version.ToString());
            writer.Write(" (");
            writer.Write(Path.GetFileName(clr.ModuleInfo.FileName));
            writer.Write(") on ");
            // ReSharper disable once HeapView.BoxingAllocation (is it worth caching?)
            writer.Write(clr.DacInfo.TargetArchitecture.ToString("G").ToLowerInvariant());
            writer.WriteLine(".");
            writer.WriteLine();
        }

        private static string ResolveSymbol(ClrRuntime runtime, Instruction instruction, long addr, ulong currentMethodAddress) {
            var operand = instruction.Operands.Length > 0 ? instruction.Operands[0] : null;
            if (operand?.PtrOffset == 0) {
                var baseOffset = instruction.PC - currentMethodAddress;
                return $"L{baseOffset + operand.PtrSegment:x4}";
            }

            return runtime.GetMethodByAddress(unchecked((ulong)addr))?.GetFullSignature();
        }

        private void DisassembleAndWrite(ClrMethod method, string message, ArchitectureMode architecture, Translator translator, Reference<ulong> methodAddressRef, TextWriter writer) {
            writer.WriteLine(method.GetFullSignature());
            if (message != null) {
                writer.Write("    ; ");
                writer.WriteLine(message);
                return;
            }

            var info = FindNonEmptyHotColdInfo(method);
            if (info == null) {
                writer.WriteLine("    ; Failed to find HotColdInfo — please report at https://github.com/ashmind/TryRoslyn/issues.");
                return;
            }

            var methodAddress = info.HotStart;
            methodAddressRef.Value = methodAddress;
            using (var disasm = new Disassembler(new IntPtr(unchecked((long)methodAddress)), (int)info.HotSize, architecture, methodAddress)) {
                foreach (var instruction in disasm.Disassemble()) {
                    writer.Write("    L");
                    writer.Write((instruction.Offset - methodAddress).ToString("x4"));
                    writer.Write(": ");
                    writer.WriteLine(translator.Translate(instruction));
                }
            }
        }

        private HotColdRegions FindNonEmptyHotColdInfo(ClrMethod method) {
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