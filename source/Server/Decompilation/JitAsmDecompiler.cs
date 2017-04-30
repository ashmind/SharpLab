using System;
using AshMind.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using AppDomainToolkit;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Runtime;
using SharpDisasm;
using SharpDisasm.Translators;
using TryRoslyn.Server.Decompilation.Support;
using IDisposable = System.IDisposable;

namespace TryRoslyn.Server.Decompilation {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class JitAsmDecompiler : IDecompiler, IDisposable {
        private readonly DataTarget _dataTarget;
        private readonly ClrRuntime _currentRuntime;

        public string LanguageName => "JIT ASM";

        public JitAsmDecompiler() {
            _dataTarget = DataTarget.AttachToProcess(CurrentProcess.Id, UInt32.MaxValue, AttachFlag.Passive);
            _currentRuntime = _dataTarget.ClrVersions.Single().CreateRuntime();
        }

        public void Decompile(Stream assemblyStream, TextWriter codeWriter) {
            var currentSetup = AppDomain.CurrentDomain.SetupInformation;
            //using (var diagnosticScope = new DiagnosticRuntimeScope())
            using (var context = AppDomainContext.Create(new AppDomainSetup {
                ApplicationBase = currentSetup.ApplicationBase,
                PrivateBinPath = currentSetup.PrivateBinPath
            })) {
                context.LoadAssembly(LoadMethod.LoadFrom, Assembly.GetExecutingAssembly().GetAssemblyFile().FullName);
                var methods = RemoteFunc.Invoke(context.Domain, assemblyStream, Remote.GetCompiledMethods);
                var translator = new IntelTranslator();
                codeWriter.WriteLine("; This is an experimental implementation.");
                codeWriter.WriteLine("; Please report any bugs to https://github.com/ashmind/TryRoslyn/issues.");
                codeWriter.WriteLine();
                foreach (var method in methods) {
                    codeWriter.WriteLine(method.FullName);
                    if (method.Pointer != null) {
                        DisassembleAndWrite(method.Pointer.Value, translator, codeWriter);
                    }
                    else {
                        codeWriter.Write("    ; ");
                        codeWriter.WriteLine(method.Message);
                    }
                    codeWriter.WriteLine();
                }
            }
        }

        private void DisassembleAndWrite(IntPtr methodPointer, Translator translator, TextWriter writer) {
            var method = _currentRuntime.GetMethodByAddress((ulong)methodPointer.ToInt64());
            var hotSize = method.HotColdInfo.HotSize;
            if (hotSize == 0) {
                writer.WriteLine("    ; Method HotSize is 0, not sure why yet.");
                writer.WriteLine("    ; See https://github.com/ashmind/TryRoslyn/issues/82.");
                return;
            }

            using (var disasm = new Disassembler(methodPointer, (int)hotSize, ArchitectureMode.x86_64)) {
                foreach (var instruction in disasm.Disassemble()) {
                    writer.Write("    0x{0:x4} ", (uint) instruction.Offset);
                    writer.WriteLine(translator.Translate(instruction));
                }
            }
        }

        private static class Remote {
            public static IReadOnlyList<MethodJitResult> GetCompiledMethods(Stream assemblyStream) {
                var assembly = Assembly.Load(ReadAllBytes(assemblyStream));
                var results = new List<MethodJitResult>();
                var reusableBuilder = new StringBuilder(300);
                foreach (var type in assembly.DefinedTypes) {
                    CompileAndCollectMembers(results, type, reusableBuilder);
                }
                return results;
            }

            private static void CompileAndCollectMembers(ICollection<MethodJitResult> results, TypeInfo type, StringBuilder reusableBuilder) {
                foreach (var constructor in type.DeclaredConstructors) {
                    results.Add(CompileAndWrap(constructor, reusableBuilder));
                }

                foreach (var method in type.DeclaredMethods) {
                    if (method.IsAbstract)
                        continue;
                    results.Add(CompileAndWrap(method, reusableBuilder));
                }
            }

            private static MethodJitResult CompileAndWrap(MethodBase method, StringBuilder reusableBuilder) {
                var fullName = GetFullName(method, reusableBuilder);
                if (method.IsGenericMethodDefinition || (method.DeclaringType?.IsGenericTypeDefinition ?? false))
                    return new MethodJitResult(fullName, "Open generics cannot be JIT-compiled.");

                var handle = method.MethodHandle;
                RuntimeHelpers.PrepareMethod(handle);

                return new MethodJitResult(fullName, handle.GetFunctionPointer());
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

            private static string GetFullName(MethodBase method, StringBuilder reusableBuilder) {
                reusableBuilder
                    .Clear()
                    .Append(method.ReflectedType?.FullName)
                    .Append("::")
                    .Append(method.Name)
                    .Append("(");

                var isFirstParameter = true;
                foreach (var parameter in method.GetParameters()) {
                    if (!isFirstParameter)
                        reusableBuilder.Append(",");

                    reusableBuilder.Append(parameter.ParameterType.FullName);
                    isFirstParameter = false;
                }
                reusableBuilder.Append(")");
                return reusableBuilder.ToString();
            }

            [Serializable]
            public struct MethodJitResult {
                public MethodJitResult(string fullName, IntPtr pointer) {
                    FullName = fullName;
                    Pointer = pointer;
                    Message = null;
                }

                public MethodJitResult(string fullName, string message) {
                    FullName = fullName;
                    Pointer = null;
                    Message = message;
                }

                public string FullName { get; }
                public IntPtr? Pointer { get; }
                public string Message { get; }
            }
        }

        public void Dispose() {
            _dataTarget.Dispose();
        }
    }
}