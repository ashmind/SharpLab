using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AppDomainToolkit;
using AshMind.Extensions;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Runtime;
using SharpDisasm;
using SharpDisasm.Translators;
using SharpLab.Runtime;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class JitAsmDecompiler : IDecompiler {
        private static readonly BindingFlags BindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

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
               
                WriteJitInfo(runtime.ClrInfo, codeWriter);

                var architecture = MapArchitecture(runtime.ClrInfo.DacInfo.TargetArchitecture);
                foreach (var result in results) {
                    var method = GetMethod(runtime, result);
                    if (method == null) {
                        codeWriter.WriteLine("Unknown (0x{0:X})", (ulong)result.Handle.ToInt64());
                        codeWriter.WriteLine("    ; Method was not found by CLRMD (reason unknown).");
                        codeWriter.WriteLine("    ; See https://github.com/ashmind/SharpLab/issues/84.");
                        continue;
                    }
                    DisassembleAndWrite(method, result.Status, architecture, translator, currentMethodAddressRef, codeWriter);
                    codeWriter.WriteLine();
                }
            }
        }

        private void WriteJitInfo(ClrInfo clr, TextWriter writer) {
            writer.WriteLine(
                "; {0:G} CLR {1} ({2}) on {3}.",
                clr.Flavor, clr.Version, Path.GetFileName(clr.ModuleInfo.FileName), clr.DacInfo.TargetArchitecture.ToString("G").ToLowerInvariant()
            );
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

        private static ClrMethod GetMethod(ClrRuntime runtime, Remote.MethodJitResult result) {
            if (result.Pointer == null)
                return runtime.GetMethodByHandle((ulong)result.Handle.ToInt64());
            return runtime.GetMethodByAddress((ulong)result.Pointer.Value.ToInt64());
        }

        private void DisassembleAndWrite(ClrMethod method, Remote.MethodJitStatus status, ArchitectureMode architecture, Translator translator, Reference<ulong> methodAddressRef, TextWriter writer) {
            writer.WriteLine(method.GetFullSignature());
            switch (status) {
                case Remote.MethodJitStatus.GenericOpenNoAttribute:
                    writer.WriteLine("    ; Open generics cannot be JIT-compiled.");
                    writer.WriteLine("    ; However you can use attribute SharpLab.Runtime.JitGeneric to specify argument types.");
                    writer.WriteLine("    ; Example: [JitGeneric(typeof(int)), JitGeneric(typeof(string))] void M<T>() { ... }.");
                    return;
            }

            var info = FindNonEmptyHotColdInfo(method);
            if (info == null) {
                if (status == Remote.MethodJitStatus.GenericSuccess) {
                    writer.WriteLine("    ; Failed to find HotColdInfo for generic method (reference types?).");
                    writer.WriteLine("    ; If you know a solution, please comment at https://github.com/ashmind/SharpLab/issues/99.");
                    return;
                }
                writer.WriteLine("    ; Failed to find HotColdInfo — please report at https://github.com/ashmind/SharpLab/issues.");
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

            private static void CompileAndCollectMembers(ICollection<MethodJitResult> results, Type type) {
                if (type.IsGenericTypeDefinition) {
                    if (TryCompileAndCollectMembersOfGeneric(results, type))
                        return;
                }

                foreach (var constructor in type.GetConstructors(BindingFlags)) {
                    CollectCompiledWraps(results, constructor);
                }

                foreach (var method in type.GetMethods(BindingFlags)) {
                    if (method.IsAbstract)
                        continue;
                    CollectCompiledWraps(results, method);
                }
            }

            private static bool TryCompileAndCollectMembersOfGeneric(ICollection<MethodJitResult> results, Type type) {
                if (type.DeclaringType?.IsGenericTypeDefinition ?? false)
                    return true; // we expect to see that one separately when we visit the parent type

                var hadAttribute = false;
                foreach (var attribute in type.GetCustomAttributes<JitGenericAttribute>(false)) {
                    hadAttribute = true;
                    var genericInstance = type.MakeGenericType(attribute.ArgumentTypes);
                    CompileAndCollectMembers(results, genericInstance.GetTypeInfo());
                    foreach (var nested in genericInstance.GetNestedTypes(BindingFlags)) {
                        CompileAndCollectMembers(results, nested);
                    }
                }
                return hadAttribute;
            }

            private static void CollectCompiledWraps(ICollection<MethodJitResult> results, MethodBase method) {
                if (method.DeclaringType?.IsGenericTypeDefinition ?? false) {
                    results.Add(new MethodJitResult(method.MethodHandle, MethodJitStatus.GenericOpenNoAttribute));
                    return;
                }

                if (method.IsGenericMethodDefinition) {
                    CollectCompiledGeneric(results, (MethodInfo)method);
                    return;
                }

                results.Add(CompileAndWrapSimple(method));
            }

            private static void CollectCompiledGeneric(ICollection<MethodJitResult> results, MethodInfo method) {
                var hasAttribute = false;
                foreach (var attribute in method.GetCustomAttributes<JitGenericAttribute>()) {
                    hasAttribute = true;
                    var genericInstance = method.MakeGenericMethod(attribute.ArgumentTypes);
                    results.Add(CompileAndWrapSimple(genericInstance));
                }
                if (!hasAttribute)
                    results.Add(new MethodJitResult(method.MethodHandle, MethodJitStatus.GenericOpenNoAttribute));
            }

            private static MethodJitResult CompileAndWrapSimple(MethodBase method) {
                var handle = method.MethodHandle;
                RuntimeHelpers.PrepareMethod(handle);
                var isGeneric = method.IsGenericMethod || (method.DeclaringType?.IsGenericType ?? false);
                return new MethodJitResult(
                    method.MethodHandle,
                    isGeneric ? MethodJitStatus.GenericSuccess : MethodJitStatus.SimpleSuccess
                );
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
                public MethodJitResult(RuntimeMethodHandle handle, MethodJitStatus status) {
                    Handle = handle.Value;
                    Pointer = status != MethodJitStatus.GenericOpenNoAttribute ? handle.GetFunctionPointer() : (IntPtr?)null;
                    Status = status;
                }

                public IntPtr Handle { get; }
                public IntPtr? Pointer { get; }
                public MethodJitStatus Status { get; }
            }

            [Serializable]
            public enum MethodJitStatus {
                SimpleSuccess,
                GenericSuccess,
                GenericOpenNoAttribute
            }
        }
    }
}