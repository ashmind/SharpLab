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
                codeWriter.WriteLine("; Please add any bugs to https://github.com/ashmind/TryRoslyn/issues/39.");
                codeWriter.WriteLine();
                foreach (var method in methods) {
                    codeWriter.WriteLine(method.FullName);
                    DisassembleAndWrite(method, translator, codeWriter);
                    codeWriter.WriteLine();
                }
            }
        }

        private void DisassembleAndWrite(Remote.JitCompiledMethod method, Translator translator, TextWriter writer) {
            var afterReturnOrThrow = false;
            using (var disasm = new Disassembler(method.Pointer, 1024 * 1024, ArchitectureMode.x86_64)) {
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
            public static IReadOnlyList<JitCompiledMethod> GetCompiledMethods(Stream assemblyStream) {
                var assembly = Assembly.Load(ReadAllBytes(assemblyStream));
                var results = new List<JitCompiledMethod>();
                var reusableBuilder = new StringBuilder(300);
                foreach (var type in assembly.DefinedTypes) {
                    CompileAndCollectMembers(results, type, reusableBuilder);
                }
                return results;
            }

            private static void CompileAndCollectMembers(ICollection<JitCompiledMethod> results, TypeInfo type, StringBuilder reusableBuilder) {
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

            private static JitCompiledMethod CompileAndWrap(MethodBase method, StringBuilder reusableBuilder) {
                var handle = method.MethodHandle;

                RuntimeHelpers.PrepareMethod(handle);
                reusableBuilder.Clear();
                AppendFullName(reusableBuilder, method);
                return new JitCompiledMethod(reusableBuilder.ToString(), handle.GetFunctionPointer());
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

            private static void AppendFullName(StringBuilder builder, MethodBase method) {
                builder.Length = 0;
                builder.Append(method.ReflectedType?.FullName);
                builder.Append("::");
                builder.Append(method.Name);
                builder.Append("(");
                var isFirstParameter = true;
                foreach (var parameter in method.GetParameters()) {
                    if (!isFirstParameter)
                        builder.Append(",");

                    builder.Append(parameter.ParameterType.FullName);
                    isFirstParameter = false;
                }
                builder.Append(")");
            }

            [Serializable]
            public struct JitCompiledMethod {
                public JitCompiledMethod(string fullName, IntPtr pointer) {
                    FullName = fullName;
                    Pointer = pointer;
                }

                public string FullName { get; }
                public IntPtr Pointer { get; }
            }
        }
    }
}