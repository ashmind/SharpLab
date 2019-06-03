using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MirrorSharp.Advanced;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common;

namespace SharpLab.Server.Execution.Internal {
    public class MemoryGraphArgumentNamesRewriter : IAssemblyRewriter {
        private readonly IReadOnlyDictionary<string, ILanguageAdapter> _languages;

        private static readonly MethodInfo AllocateNextMethod =
            typeof(MemoryGraphArgumentNames).GetMethod(nameof(MemoryGraphArgumentNames.AllocateNext));
        private static readonly MethodInfo AddToNextMethod =
            typeof(MemoryGraphArgumentNames).GetMethod(nameof(MemoryGraphArgumentNames.AddToNext));

        public MemoryGraphArgumentNamesRewriter(IReadOnlyList<ILanguageAdapter> languages) {
            _languages = languages.ToDictionary(l => l.LanguageName);
        }

        public void Rewrite(AssemblyDefinition assembly, IWorkSession session) {
            foreach (var module in assembly.Modules) {
                var argumentMethods = new ArgumentMethods {
                    AllocateNext = module.ImportReference(AllocateNextMethod),
                    AddToNext = module.ImportReference(AddToNextMethod)
                };

                foreach (var type in module.Types) {
                    Rewrite(type, session, argumentMethods);
                    foreach (var nested in type.NestedTypes) {
                        Rewrite(nested, session, argumentMethods);
                    }
                }
            }
        }

        private void Rewrite(TypeDefinition type, IWorkSession session, ArgumentMethods argumentMethods) {
            foreach (var method in type.Methods) {
                Rewrite(method, session, argumentMethods);
            }
        }

        private void Rewrite(MethodDefinition method, IWorkSession session, ArgumentMethods argumentMethods) {
            if (!method.HasBody || method.Body.Instructions.Count == 0)
                return;

            var il = method.Body.GetILProcessor();
            var instructions = il.Body.Instructions;
            for (var i = instructions.Count - 1; i >= 0; i--) {
                var instruction = instructions[i];
                var code = instruction.OpCode.Code;
                if (code != Code.Call)
                    continue;

                var callee = (instruction.Operand as MethodReference)?.Resolve();
                if (callee == null || !IsInspectMemoryGraph(callee))
                    continue;

                RewriteInspectMemoryGraph(method, il, instruction, session, argumentMethods);
            }
        }

        private void RewriteInspectMemoryGraph(MethodDefinition method, ILProcessor il, Instruction call, IWorkSession session, ArgumentMethods argumentMethods) {
            var sequencePoint = FindClosestSequencePoint(method, call);
            if (sequencePoint == null)
                return;

            var arguments = _languages[session.LanguageName]
                .GetCallArgumentIdentifiers(session, sequencePoint.StartLine, sequencePoint.StartColumn);

            if (arguments.Length == 0)
                return;

            il.InsertBefore(call, il.CreateLdcI4Best(arguments.Length));
            il.InsertBefore(call, il.CreateCall(argumentMethods.AllocateNext));
            foreach (var argument in arguments) {
                il.InsertBefore(call, argument != null ? il.Create(OpCodes.Ldstr, argument) : il.Create(OpCodes.Ldnull));
                il.InsertBefore(call, il.CreateCall(argumentMethods.AddToNext));
            }
        }

        private SequencePoint? FindClosestSequencePoint(MethodDefinition method, Instruction call) {
            var debug = method.DebugInformation;
            if (debug == null || !debug.HasSequencePoints)
                return null;

            var points = debug.SequencePoints;
            SequencePoint? candidate = null;
            foreach (var sequencePoint in debug.SequencePoints) {
                if (call.Offset < sequencePoint.Offset)
                    break;
                candidate = sequencePoint;
            }

            return candidate;
        }

        private static bool IsInspectMemoryGraph(MethodDefinition method) {
            return method.Name == nameof(Inspect.MemoryGraph)
                && method.DeclaringType.Name == nameof(Inspect)
                && (method.DeclaringType.Namespace ?? "") == (typeof(Inspect).Namespace ?? "");
        }

        private struct ArgumentMethods {
            public MethodReference AllocateNext { get; set; }
            public MethodReference AddToNext { get; set; }
        }
    }
}
