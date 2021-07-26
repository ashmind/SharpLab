using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Iced.Intel;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.DacInterface;
using SharpLab.Runtime;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class JitAsmDecompiler : IDecompiler {
        private static readonly FormatterOptions FormatterOptions = new() {
            HexPrefix = "0x",
            HexSuffix = null,
            UppercaseHex = false,
            SpaceAfterOperandSeparator = true
        };
        private readonly Pool<ClrRuntime> _runtimePool;

        public string LanguageName => TargetNames.JitAsm;

        public JitAsmDecompiler(Pool<ClrRuntime> runtimePool) {
            _runtimePool = runtimePool;
        }

        public void Decompile(CompilationStreamPair streams, TextWriter codeWriter) {
            Argument.NotNull(nameof(streams), streams);
            Argument.NotNull(nameof(codeWriter), codeWriter);

            using var loadContext = new CustomAssemblyLoadContext(shouldShareAssembly: _ => true); var assembly = loadContext.LoadFromStream(streams.AssemblyStream);
            ValidateStaticConstructors(assembly);

            using var runtimeLease = _runtimePool.GetOrCreate();
            var runtime = runtimeLease.Object;

            runtime.FlushCachedData();
            var context = new JitWriteContext(codeWriter, runtime);

            WriteJitInfo(runtime.ClrInfo, codeWriter);
            WriteProfilerState(codeWriter);

            foreach (var type in assembly.DefinedTypes) {
                if (type.IsNested)
                    continue; // it's easier to handle nested generic types recursively, so we suppress all nested for consistency
                DisassembleAndWriteMembers(context, type);
            }
        }

        private void ValidateStaticConstructors(Assembly assembly) {
            foreach (var type in assembly.DefinedTypes) {
                foreach (var constructor in type.DeclaredConstructors) {
                    if (constructor.IsStatic)
                        throw new NotSupportedException($"Type {type} has a static constructor, which is not supported by SharpLab JIT decompiler.");
                }
            }
        }

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

        private void DisassembleAndWriteMembers(JitWriteContext context, TypeInfo type, ImmutableArray<Type>? genericArgumentTypes = null) {
            if (type.IsGenericTypeDefinition) {
                if (TryDisassembleAndWriteMembersOfGeneric(context, type, genericArgumentTypes))
                    return;
            }

            foreach (var constructor in type.DeclaredConstructors) {
                DisassembleAndWriteMethod(context, constructor);
            }

            foreach (var method in type.DeclaredMethods) {
                if (method.IsAbstract)
                    continue;
                DisassembleAndWriteMethod(context, method);
            }

            foreach (var nested in type.DeclaredNestedTypes) {
                DisassembleAndWriteMembers(context, nested, genericArgumentTypes);
            }
        }

        private bool TryDisassembleAndWriteMembersOfGeneric(JitWriteContext context, TypeInfo type, ImmutableArray<Type>? parentArgumentTypes = null) {
            var hadAttribute = false;
            foreach (var attribute in type.GetCustomAttributes<JitGenericAttribute>(false)) {
                hadAttribute = true;

                var fullArgumentTypes = (parentArgumentTypes ?? ImmutableArray<Type>.Empty)
                    .AddRange(attribute.ArgumentTypes);
                var genericInstance = ApplyJitGenericAttribute<Type>(type, fullArgumentTypes.ToArray(), static (t, a) => t.MakeGenericType(a));
                DisassembleAndWriteMembers(context, genericInstance.GetTypeInfo(), fullArgumentTypes);
            }
            if (hadAttribute)
                return true;

            if (parentArgumentTypes != null) {
                var genericInstance = ApplyJitGenericAttribute<Type>(type, parentArgumentTypes.Value.ToArray(), static (t, a) => t.MakeGenericType(a));
                DisassembleAndWriteMembers(context, genericInstance.GetTypeInfo(), parentArgumentTypes);
                return true;
            }

            return false;
        }

        private void DisassembleAndWriteMethod(JitWriteContext context, MethodBase method) {
            if ((method.MethodImplementationFlags & MethodImplAttributes.Runtime) == MethodImplAttributes.Runtime) {
                WriteSignatureFromReflection(context, method);
                context.Writer.WriteLine("    ; Cannot produce JIT assembly for runtime-implemented method.");
                return;
            }

            if (method.DeclaringType?.IsGenericTypeDefinition ?? false) {
                WriteIgnoredOpenGeneric(context, method);
                return;
            }

            if (method.IsGenericMethodDefinition) {
                DisassembleAndWriteGenericMethod(context, (MethodInfo)method);
                return;
            }

            DisassembleAndWriteSimpleMethod(context, method);
        }

        private void DisassembleAndWriteGenericMethod(JitWriteContext context, MethodInfo method) {
            var hasAttribute = false;
            foreach (var attribute in method.GetCustomAttributes<JitGenericAttribute>()) {
                hasAttribute = true;
                var genericInstance = ApplyJitGenericAttribute(method, attribute.ArgumentTypes, static (m, a) => m.MakeGenericMethod(a));
                DisassembleAndWriteSimpleMethod(context, genericInstance);
            }
            if (!hasAttribute)
                WriteIgnoredOpenGeneric(context, method);
        }

        private void DisassembleAndWriteSimpleMethod(JitWriteContext context, MethodBase method) {
            var handle = method.MethodHandle;
            RuntimeHelpers.PrepareMethod(handle);

            var clrMethodData = FindJitCompiledMethod(context, handle);

            var writer = context.Writer;
            if (clrMethodData?.Signature is {} signature) {
                writer.WriteLine();
                writer.WriteLine(signature);
            }
            else {
                WriteSignatureFromReflection(context, method);
            }

            if (clrMethodData == null) {
                if (method.IsGenericMethod) {
                    writer.WriteLine("    ; Failed to find JIT output for generic method (reference types?).");
                    writer.WriteLine("    ; If you know a solution, please comment at https://github.com/ashmind/SharpLab/issues/99.");
                    return;
                }

                writer.WriteLine("    ; Failed to find JIT output — please report at https://github.com/ashmind/SharpLab/issues.");
                return;
            }

            var methodAddress = clrMethodData.Value.MethodAddress;
            var methodLength = clrMethodData.Value.MethodSize;

            var reader = new MemoryCodeReader(new IntPtr(unchecked((long)methodAddress)), methodLength);
            var decoder = Decoder.Create(MapArchitectureToBitness(context.Runtime.DataTarget!.DataReader.Architecture), reader);

            var instructions = new InstructionList();
            decoder.IP = methodAddress;
            while (decoder.IP < (methodAddress + methodLength)) {
                decoder.Decode(out instructions.AllocUninitializedElement());
            }

            var resolver = new JitAsmSymbolResolver(context.Runtime, methodAddress, methodLength);
            var formatter = new IntelFormatter(FormatterOptions, resolver);
            var output = new StringOutput();
            foreach (ref var instruction in instructions) {
                formatter.Format(instruction, output);

                writer.Write("    L");
                writer.Write((instruction.IP - methodAddress).ToString("x4"));
                writer.Write(": ");
                writer.WriteLine(output.ToStringAndReset());
            }
        }

        private ClrMethodData? FindJitCompiledMethod(JitWriteContext context, RuntimeMethodHandle handle) {
            context.Runtime.FlushCachedData();
            var sos = context.Runtime.DacLibrary.SOSDacInterface;

            var methodDescAddress = unchecked((ulong)handle.Value.ToInt64());
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

        private void WriteIgnoredOpenGeneric(JitWriteContext context, MethodBase method) {
            WriteSignatureFromReflection(context, method);
            var writer = context.Writer;
            writer.WriteLine("    ; Open generics cannot be JIT-compiled.");
            writer.WriteLine("    ; However you can use attribute SharpLab.Runtime.JitGeneric to specify argument types.");
            writer.WriteLine("    ; Example: [JitGeneric(typeof(int)), JitGeneric(typeof(string))] void M<T>() { ... }.");
        }

        private void WriteSignatureFromReflection(JitWriteContext context, MethodBase method) {
            context.Writer.WriteLine();

            var md = (ulong)method.MethodHandle.Value.ToInt64();
            var signature = context.Runtime.DacLibrary.SOSDacInterface.GetMethodDescName(md);

            context.Writer.WriteLine(signature ?? "Unknown Method");
        }

        private TMember ApplyJitGenericAttribute<TMember>(TMember definition, Type[] arguments, Func<TMember, Type[], TMember> makeGeneric)
            where TMember : MemberInfo
        {
            try {
                return makeGeneric(definition, arguments);
            }
            catch (ArgumentException ex) {
                throw new JitGenericAttributeException($"Failed to apply JitGenericAttribute to {definition.Name}: {ex.Message}", ex);
            }
            catch (Exception ex) when (
                ex is BadImageFormatException or TypeLoadException
                && arguments.FirstOrDefault(static a => a.IsByRefLike) is {} refStructArgument
            ) {
                throw new JitGenericAttributeException($"JitGenericAttribute argument {refStructArgument.Name} is a ref struct, which is not supported in generics", ex);
            }
        }

        private int MapArchitectureToBitness(Architecture architecture) => architecture switch
        {
            Architecture.Amd64 => 64,
            Architecture.X86 => 32,
            _ => throw new Exception($"Unsupported architecture {architecture}.")
        };

        private class JitWriteContext {
            public JitWriteContext(TextWriter writer, ClrRuntime runtime) {
                Writer = writer;
                Runtime = runtime;
            }
            
            public TextWriter Writer { get; }
            public ClrRuntime Runtime { get; }
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