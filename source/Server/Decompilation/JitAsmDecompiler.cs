using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using SharpLab.Server.Common;

namespace SharpLab.Server.Decompilation {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class JitAsmDecompiler : IDecompiler {
        public string LanguageName => TargetNames.JitAsm;

        public void Decompile(CompilationStreamPair streams, TextWriter codeWriter) {
            Argument.NotNull(nameof(streams), streams);
            Argument.NotNull(nameof(codeWriter), codeWriter);

            var currentSetup = AppDomain.CurrentDomain.SetupInformation;
            using (var dataTarget = DataTarget.AttachToProcess(CurrentProcess.Id, UInt32.MaxValue, AttachFlag.Passive))
            using (var context = AppDomainContext.Create(new AppDomainSetup {
                ApplicationBase = currentSetup.ApplicationBase,
                PrivateBinPath = currentSetup.PrivateBinPath
            })) {
                context.LoadAssembly(LoadMethod.LoadFrom, Assembly.GetExecutingAssembly().GetAssemblyFile().FullName);
                var results = RemoteFunc.Invoke(context.Domain, streams.AssemblyStream, Remote.GetCompiledMethods);

                var currentMethodAddressRef = new Reference<ulong>();
                var runtime = dataTarget.ClrVersions.Single().CreateRuntime();
                var translator = new IntelTranslator {
                    SymbolResolver = (Instruction instruction, long addr, ref long offset) => 
                        ResolveSymbol(runtime, instruction, addr, currentMethodAddressRef.Value)
                };

                WriteJitInfo(runtime.ClrInfo, codeWriter);

                var architecture = MapArchitecture(runtime.ClrInfo.DacInfo.TargetArchitecture);
                foreach (var result in results) {
                    DisassembleAndWrite(result, runtime, architecture, translator, currentMethodAddressRef, codeWriter);
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
                var lvalue = GetOperandLValue(operand);
                if (lvalue == null)
                    return $"{operand.RawValue} ; failed to resolve lval ({operand.Size}), please report at https://github.com/ashmind/SharpLab/issues";
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

        private void DisassembleAndWrite(Remote.MethodJitResult result, ClrRuntime runtime, ArchitectureMode architecture, Translator translator, Reference<ulong> methodAddressRef, TextWriter writer) {
            var (method, regions) = ResolveJitResult(runtime, result);
            if (method == null) {
                writer.WriteLine("Unknown (0x{0:X})", (ulong)result.Handle.ToInt64());
                writer.WriteLine("    ; Method was not found by CLRMD (reason unknown).");
                writer.WriteLine("    ; See https://github.com/ashmind/SharpLab/issues/84.");
                return;
            }

            writer.WriteLine(method.GetFullSignature());
            switch (result.Status) {
                case Remote.MethodJitStatus.IgnoredRuntime:
                    writer.WriteLine("    ; Cannot produce JIT assembly for runtime-implemented method.");
                    return;
                case Remote.MethodJitStatus.IgnoredOpenGenericWithNoAttribute:
                    writer.WriteLine("    ; Open generics cannot be JIT-compiled.");
                    writer.WriteLine("    ; However you can use attribute SharpLab.Runtime.JitGeneric to specify argument types.");
                    writer.WriteLine("    ; Example: [JitGeneric(typeof(int)), JitGeneric(typeof(string))] void M<T>() { ... }.");
                    return;
            }

            if (regions == null) {
                if (result.Status == Remote.MethodJitStatus.SuccessGeneric) {
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

        private (ClrMethod method, HotColdRegions regions) ResolveJitResult(ClrRuntime runtime, Remote.MethodJitResult result) {
            ClrMethod methodByPointer = null;
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
                // This is a security consideration as PrepareMethod calls static ctors
                foreach (var type in assembly.DefinedTypes) {
                    foreach (var constructor in type.DeclaredConstructors) {
                        if (constructor.IsStatic)
                            throw new NotSupportedException("Type " + type + " has a static constructor, which is not supported by SharpLab JIT decompiler.");
                    }
                }
                var results = new List<MethodJitResult>();
                foreach (var type in assembly.DefinedTypes) {
                    if (type.IsNested)
                        continue; // it's easier to handle nested generic types recursively, so we suppress all nested for consistency
                    CompileAndCollectMembers(results, type);
                }
                return results;
            }

            private static void CompileAndCollectMembers(ICollection<MethodJitResult> results, TypeInfo type, ImmutableArray<Type>? genericArgumentTypes = null) {
                if (type.IsGenericTypeDefinition) {
                    if (TryCompileAndCollectMembersOfGeneric(results, type, genericArgumentTypes))
                        return;
                }

                foreach (var constructor in type.DeclaredConstructors) {
                    CollectCompiledWraps(results, constructor);
                }

                foreach (var method in type.DeclaredMethods) {
                    if (method.IsAbstract)
                        continue;
                    CollectCompiledWraps(results, method);
                }

                foreach (var nested in type.DeclaredNestedTypes) {
                    CompileAndCollectMembers(results, nested, genericArgumentTypes);
                }
            }

            private static bool TryCompileAndCollectMembersOfGeneric(ICollection<MethodJitResult> results, TypeInfo type, ImmutableArray<Type>? parentArgumentTypes = null) {
                var hadAttribute = false;
                foreach (var attribute in type.GetCustomAttributes<JitGenericAttribute>(false)) {
                    hadAttribute = true;

                    var fullArgumentTypes = (parentArgumentTypes ?? ImmutableArray<Type>.Empty)
                        .AddRange(attribute.ArgumentTypes);
                    var genericInstance = type.MakeGenericType(fullArgumentTypes.ToArray());
                    CompileAndCollectMembers(results, genericInstance.GetTypeInfo(), fullArgumentTypes);
                }
                if (hadAttribute)
                    return true;

                if (parentArgumentTypes != null) {
                    var genericInstance = type.MakeGenericType(parentArgumentTypes.Value.ToArray());
                    CompileAndCollectMembers(results, genericInstance.GetTypeInfo(), parentArgumentTypes);
                    return true;
                }

                return false;
            }

            private static void CollectCompiledWraps(ICollection<MethodJitResult> results, MethodBase method) {
                if ((method.MethodImplementationFlags & MethodImplAttributes.Runtime) == MethodImplAttributes.Runtime) {
                    results.Add(new MethodJitResult(method.MethodHandle, MethodJitStatus.IgnoredRuntime));
                    return;
                }

                if (method.DeclaringType?.IsGenericTypeDefinition ?? false) {
                    results.Add(new MethodJitResult(method.MethodHandle, MethodJitStatus.IgnoredOpenGenericWithNoAttribute));
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
                    results.Add(new MethodJitResult(method.MethodHandle, MethodJitStatus.IgnoredOpenGenericWithNoAttribute));
            }

            private static MethodJitResult CompileAndWrapSimple(MethodBase method) {
                var handle = method.MethodHandle;
                RuntimeHelpers.PrepareMethod(handle);
                var isGeneric = method.IsGenericMethod || (method.DeclaringType?.IsGenericType ?? false);
                return new MethodJitResult(method.MethodHandle, isGeneric ? MethodJitStatus.SuccessGeneric : MethodJitStatus.Success);
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
                    Pointer = GetIsSuccess(status)
                            ? handle.GetFunctionPointer()
                            : (IntPtr?)null;
                    Status = status;
                }

                public IntPtr Handle { get; }
                public IntPtr? Pointer { get; }
                public MethodJitStatus Status { get; }

                public bool IsSuccess => GetIsSuccess(Status);
                private static bool GetIsSuccess(MethodJitStatus status) {
                    return status == MethodJitStatus.Success
                        || status == MethodJitStatus.SuccessGeneric;
                }
            }

            [Serializable]
            public enum MethodJitStatus {
                Success,
                SuccessGeneric,
                IgnoredRuntime,
                IgnoredOpenGenericWithNoAttribute
            }
        }
    }
}