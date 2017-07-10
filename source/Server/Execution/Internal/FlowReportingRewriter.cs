using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SharpLab.Runtime.Internal;

namespace SharpLab.Server.Execution.Internal {
    public class FlowReportingRewriter {
        private const int HiddenLine = 0xFEEFEE;

        private static readonly MethodInfo ReportVariableMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportVariable));
        private static readonly MethodInfo ReportLineStartMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportLineStart));

        public void Rewrite(AssemblyDefinition assembly) {
            foreach (var module in assembly.Modules) {
                var reportVariable = module.ImportReference(ReportVariableMethod);
                var reportLineStart = module.ImportReference(ReportLineStartMethod);
                foreach (var type in module.Types) {
                    Rewrite(type, reportVariable, reportLineStart);
                    foreach (var nested in type.NestedTypes) {
                        Rewrite(nested, reportVariable, reportLineStart);
                    }
                }
            }
        }

        private void Rewrite(TypeDefinition type, MethodReference reportVariable, MethodReference reportLineStart) {
            foreach (var method in type.Methods) {
                Rewrite(method, reportVariable, reportLineStart);
            }
        }

        private void Rewrite(MethodDefinition method, MethodReference reportVariable, MethodReference reportLineStart) {
            if (!method.HasBody || method.Body.Instructions.Count == 0)
                return;

            var il = method.Body.GetILProcessor();
            var instructions = il.Body.Instructions;
            var lastLine = (int?)null;
            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];

                var sequencePoint = instruction.SequencePoint;
                if (sequencePoint != null  && sequencePoint.StartLine != HiddenLine && sequencePoint.StartLine != lastLine) {
                    InsertBefore(il, instruction, il.Create(OpCodes.Ldc_I4, sequencePoint.StartLine));
                    InsertBefore(il, instruction, il.Create(OpCodes.Call, reportLineStart));
                    i += 2;
                    lastLine = sequencePoint.StartLine;
                }

                var localIndex = GetIndexIfStloc(instruction);
                if (localIndex == null)
                    continue;

                var variable = il.Body.Variables[localIndex.Value];
                if (string.IsNullOrEmpty(variable.Name))
                    continue;

                var closestSequencePoint = sequencePoint ?? FindSequencePoint(instruction);
                var insertTarget = instruction;
                InsertAfter(il, ref insertTarget, ref i, il.Create(OpCodes.Ldstr, variable.Name));
                InsertAfter(il, ref insertTarget, ref i, il.Create(OpCodes.Ldloc, localIndex.Value));
                InsertAfter(il, ref insertTarget, ref i, il.Create(OpCodes.Ldc_I4, closestSequencePoint.EndLine));
                InsertAfter(il, ref insertTarget, ref i, il.Create(OpCodes.Call, new GenericInstanceMethod(reportVariable) {
                    GenericArguments = { variable.VariableType }
                }));
            }
        }

        private void InsertAfter(ILProcessor il, ref Instruction target, ref int index, Instruction instruction) {
            il.InsertAfter(target, instruction);
            target = instruction;
            index += 1;
        }

        public void InsertBefore(ILProcessor il, Instruction target, Instruction instruction) {
            il.InsertBefore(target, instruction);
            foreach (var other in il.Body.Instructions) {
                if (other.Operand == target)
                    other.Operand = instruction;
            }

            if (!il.Body.HasExceptionHandlers)
                return;

            foreach (var handler in il.Body.ExceptionHandlers) {
                if (handler.TryStart == target)
                    handler.TryStart = instruction;
                if (handler.TryEnd == target)
                    handler.TryEnd = instruction;
                if (handler.HandlerStart == target)
                    handler.HandlerStart = instruction;
                if (handler.HandlerEnd == target)
                    handler.HandlerEnd = instruction;
            }
        }

        private int? GetIndexIfStloc(Instruction instruction) {
            switch (instruction.OpCode.Code) {
                case Code.Stloc_0: return 0;
                case Code.Stloc_1: return 1;
                case Code.Stloc_2: return 2;
                case Code.Stloc_3: return 3;
                case Code.Stloc: return (int)instruction.Operand;
                default: return null;
            }
        }

        private SequencePoint FindSequencePoint(Instruction instruction) {
            var current = instruction;
            while (current.SequencePoint == null || current.SequencePoint.StartLine == HiddenLine) {
                current = current.Previous;
            }
            return current.SequencePoint;
        }
    }
}
