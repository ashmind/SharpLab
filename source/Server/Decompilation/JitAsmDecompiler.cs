using System;
using AshMind.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using AppDomainToolkit;
using JetBrains.Annotations;
using SharpDisasm;
using SharpDisasm.Translators;
using SharpDisasm.Udis86;

namespace TryRoslyn.Server.Decompilation {
    using static ud_mnemonic_code;

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class JitAsmDecompiler : IDecompiler {
        public string LanguageName => "JIT ASM";

        private static readonly HashSet<ud_mnemonic_code> Jumps = new HashSet<ud_mnemonic_code> {
            UD_Ija,
            UD_Ijae,
            UD_Ijb,
            UD_Ijbe,
            UD_Ijcxz,
            UD_Ijecxz,
            UD_Ijg,
            UD_Ijge,
            UD_Ijl,
            UD_Ijle,
            UD_Ijmp,
            UD_Ijno,
            UD_Ijnp,
            UD_Ijns,
            UD_Ijnz,
            UD_Ijo,
            UD_Ijp,
            UD_Ijs,
            UD_Ijz
        };

        public void Decompile(Stream assemblyStream, TextWriter codeWriter) {
            var currentSetup = AppDomain.CurrentDomain.SetupInformation;
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
            var afterReturnOrThrow = false;
            using (var disasm = new Disassembler(methodPointer, 1024 * 1024, ArchitectureMode.x86_64)) {
                HashSet<ulong> jumpOffsets = null;
                foreach (var instruction in disasm.Disassemble()) {
                    if (afterReturnOrThrow) {
                        if (!(jumpOffsets?.Contains(instruction.Offset) ?? false))
                            break;
                        afterReturnOrThrow = false;
                    }
                    writer.Write("    0x{0:x4} ", (uint) instruction.Offset);
                    writer.WriteLine(translator.Translate(instruction));
                    if (Jumps.Contains(instruction.Mnemonic)) {
                        var operand = instruction.Operands[0];
                        if (operand.PtrOffset == 0) {
                            jumpOffsets = jumpOffsets ?? new HashSet<ulong>();
                            jumpOffsets.Add(instruction.PC + operand.PtrSegment);
                        }
                    }

                    if (instruction.Mnemonic == UD_Iret || instruction.Mnemonic == UD_Iint3)
                        afterReturnOrThrow = true;
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

                foreach (var nested in type.DeclaredNestedTypes) {
                    CompileAndCollectMembers(results, nested, reusableBuilder);
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
    }
}