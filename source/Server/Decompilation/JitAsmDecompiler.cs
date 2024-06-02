using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AshMind.Extensions;
using Iced.Intel;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Runtime;
using MirrorSharp.Advanced;
using SharpLab.Runtime;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Diagnostics;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public class JitAsmDecompiler : IDecompiler {
    private static readonly FormatterOptions FormatterOptions = new() {
        HexPrefix = "0x",
        HexSuffix = null,
        UppercaseHex = false,
        SpaceAfterOperandSeparator = true
    };
    private readonly Pool<ClrRuntime> _runtimePool;
    private readonly JitAsmSettings _settings;

    public string LanguageName => TargetNames.JitAsm;

    public JitAsmDecompiler(Pool<ClrRuntime> runtimePool, JitAsmSettings settings) {
        _runtimePool = runtimePool;
        _settings = settings;
    }

    public void Decompile(CompilationStreamPair streams, TextWriter codeWriter, IWorkSession session) {
        Argument.NotNull(nameof(streams), streams);
        Argument.NotNull(nameof(codeWriter), codeWriter);
        Argument.NotNull(nameof(session), session);

        using var loadContext = new CustomAssemblyLoadContext(shouldShareAssembly: _ => true);
        var assembly = loadContext.LoadFromStream(streams.AssemblyStream);
        EnsureNoJitSideEffects(assembly);

        using var runtimeLease = _runtimePool.GetOrCreate();
        var runtime = runtimeLease.Object;

        runtime.FlushCachedData();
        var context = new JitWriteContext(codeWriter, runtime);

        WriteJitInfo(runtime.ClrInfo, codeWriter);
        WriteProfilerState(codeWriter);

        DisassembleAndWriteTypesInOrder(context, assembly);
    }

    private void EnsureNoJitSideEffects(Assembly assembly) {
        try {
            foreach (var type in assembly.DefinedTypes) {
                foreach (var constructor in type.DeclaredConstructors) {
                    if (constructor.IsStatic)
                        throw new NotSupportedException($"Type {type} has a static constructor, which is not supported by SharpLab JIT decompiler.");
                }

                foreach (var method in type.DeclaredMethods) {
                    foreach (var attribute in method.CustomAttributes) {
                        if (attribute.AttributeType is { Name: "ModuleInitializerAttribute", Namespace: "System.Runtime.CompilerServices" })
                            throw new NotSupportedException($"Method {method} is a module initializer, which is not supported by SharpLab JIT decompiler.");
                    }
                }
            }
        }
        catch (ReflectionTypeLoadException ex) {
            throw new NotSupportedException("Unable to validate whether code has static constructors or module initializers (not supported by SharpLab JIT decompiler).", ex);
        }
    }

    private void WriteJitInfo(ClrInfo clr, TextWriter writer) {
        writer.WriteLine(
            "; {0:G} CLR {1} on {2}",
            clr.Flavor, clr.Version, clr.DataTarget.DataReader.Architecture.ToString("G").ToLowerInvariant()
        );
    }

    private void WriteProfilerState(TextWriter writer) {
        if (!ProfilerState.Active)
            return;

        writer.WriteLine("; Note: Running under profiler, which affects JIT assembly in heap allocations.");
    }

    private void DisassembleAndWriteTypesInOrder(JitWriteContext context, Assembly assembly) {
        var lastNonUserTypeIndex = -1;
        var types = assembly.GetTypes();
        for (var i = 0; i < types.Length; i++) {
            var type = types[i];

            if (type.IsNested)
                continue; // it's easier to handle nested generic types recursively, so we suppress all nested for consistency

            if (IsNonUserCode(type)) {
                lastNonUserTypeIndex = i;
                continue;
            }

            DisassembleAndWriteMembers(context, type.GetTypeInfo());
        }

        if (lastNonUserTypeIndex >= 0) {
            for (var i = 0; i <= lastNonUserTypeIndex; i++) {
                DisassembleAndWriteMembers(context, types[i].GetTypeInfo());
            }
        }
    }

    private bool IsNonUserCode(Type type) {
        // Note: the logic cannot be reused, but should match C# and IL
        return type.Namespace != null
            && type.IsDefined<CompilerGeneratedAttribute>();
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

            var fullArgumentTypes = (parentArgumentTypes ?? [])
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
        #if DEBUG
        DiagnosticLog.LogMessage($"[JitAsm] Processing method {method.Name}");
        #endif

        if ((method.MethodImplementationFlags & MethodImplAttributes.Runtime) == MethodImplAttributes.Runtime) {
            WriteSignatureFromReflection(context, method);
            context.Writer.WriteLine("    ; Cannot produce JIT assembly for runtime-implemented method.");
            return;
        }

        if ((method.MethodImplementationFlags & MethodImplAttributes.InternalCall) == MethodImplAttributes.InternalCall) {
            WriteSignatureFromReflection(context, method);
            context.Writer.WriteLine("    ; Cannot produce JIT assembly for an internal call method.");
            return;
        }

        if ((method.Attributes & MethodAttributes.PinvokeImpl) == MethodAttributes.PinvokeImpl) {
            WriteSignatureFromReflection(context, method);
            context.Writer.WriteLine("    ; Cannot produce JIT assembly for a P/Invoke method.");
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

        var clrMethodData = FindJitCompiledMethod(context.Runtime, method.MethodHandle);

        var writer = context.Writer;
        if (clrMethodData?.Signature is {} signature) {
            writer.WriteLine();
            writer.WriteLine(signature);
        }
        else {
            WriteSignatureFromReflection(context, method);
        }

        if (clrMethodData == null) {
            writer.WriteLine("    ; Failed to find JIT output. This might appear more frequently than before due to a library update.");
            writer.WriteLine("    ; Please monitor https://github.com/ashmind/SharpLab/issues/1334 for progress.");
            return;
        }

        var methodAddress = clrMethodData.Value.MethodAddress;
        var methodLength = clrMethodData.Value.MethodSize;

        var reader = new MemoryCodeReader(new IntPtr(unchecked((long)methodAddress)), methodLength);
        var decoder = Decoder.Create(MapArchitectureToBitness(context.Runtime.DataTarget.DataReader.Architecture), reader);

        var instructions = new InstructionList();
        decoder.IP = methodAddress;
        while (decoder.IP < (methodAddress + methodLength)) {
            decoder.Decode(out instructions.AllocUninitializedElement());
        }

        var resolver = new JitAsmSymbolResolver(context.Runtime, methodAddress, methodLength, _settings);
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

    private ClrMethodData? FindJitCompiledMethod(ClrRuntime runtime, RuntimeMethodHandle handle) {
        lock (runtime)
            runtime.FlushCachedData();

        var methodDescAddress = unchecked((ulong)handle.Value.ToInt64());
        if (runtime.GetMethodByHandle(methodDescAddress) is not { } method) {
            #if DEBUG
            DiagnosticLog.LogMessage($"[JitAsm] Failed to GetMethodByHandle(0x{methodDescAddress:X}).");
            #endif
            return null;
        }

        if (method.CompilationType == MethodCompilationType.None) {
            #if DEBUG
            DiagnosticLog.LogMessage($"[JitAsm] Method {method.Signature} compilation type is None.");
            #endif
            return null;
        }

        if (method.NativeCode == 0) {
            #if DEBUG
            DiagnosticLog.LogMessage($"[JitAsm] Method {method.Signature} native code is 0.");
            #endif
            return null;
        }

        if (method.HotColdInfo.HotSize == 0) {
            #if DEBUG
            DiagnosticLog.LogMessage($"[JitAsm] Method {method.Signature} hot size is 0.");
            #endif
            return null;
        }

        return new(
            method.Signature,
            method.NativeCode,
            method.HotColdInfo.HotSize
        );
    }

    private void WriteIgnoredOpenGeneric(JitWriteContext context, MethodBase method) {
        WriteSignatureFromReflection(context, method);
        var writer = context.Writer;
        writer.WriteLine("    ; Open generics cannot be JIT-compiled.");
        writer.WriteLine("    ; However you can use attribute SharpLab.Runtime.JitGeneric to specify argument types.");
        writer.WriteLine("    ; Example: [JitGeneric(typeof(int)), JitGeneric(typeof(string))] void M<T>() { ... }.");
    }

    private void WriteSignatureFromReflection(JitWriteContext context, MethodBase method) {
        var writer = context.Writer;

        writer.WriteLine();
        if (method.DeclaringType is { } declaringType) {
            writer.Write(declaringType.FullName);
            writer.Write(".");
        }

        writer.Write(method.Name);
        if (method.IsGenericMethod) {
            writer.Write("[[");
            var first = true;
            foreach (var type in method.GetGenericArguments()) {
                if (first) {
                    first = false;
                }
                else {
                    writer.Write(", ");
                }
                writer.Write(type.FullName);
                writer.Write(", ");
                writer.Write(type.Assembly.GetName().Name);
            }
            writer.Write("]]");
        }

        writer.WriteLine(method.GetParameters().Length > 0 ? "(...)" : "()");
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
            throw new JitGenericAttributeException($"JitGenericAttribute argument {refStructArgument.Name} is a ref struct, which is not supported in generics.", ex);
        }
    }

    private int MapArchitectureToBitness(Architecture architecture) => architecture switch
    {
        Architecture.X64 => 64,
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