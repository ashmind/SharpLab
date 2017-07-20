using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SharpLab.Runtime.Internal;

namespace SharpLab.Server.Execution.Internal {
    public class FlowReportingRewriter {
        private const int HiddenLine = 0xFEEFEE;

        private static readonly MethodInfo ReportLineStartMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportLineStart));
        private static readonly MethodInfo ReportVariableMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportVariable));
        private static readonly MethodInfo ReportExceptionMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportException));

        public void Rewrite(AssemblyDefinition assembly) {
            foreach (var module in assembly.Modules) {
                var flow = new ReportMethods {
                    ReportLineStart = module.ImportReference(ReportLineStartMethod),
                    ReportVariable = module.ImportReference(ReportVariableMethod),
                    ReportException = module.ImportReference(ReportExceptionMethod),
                };
                foreach (var type in module.Types) {
                    Rewrite(type, flow);
                    foreach (var nested in type.NestedTypes) {
                        Rewrite(nested, flow);
                    }
                }
            }
        }

        private void Rewrite(TypeDefinition type, ReportMethods flow) {
            foreach (var method in type.Methods) {
                Rewrite(method, flow);
            }
        }

        private void Rewrite(MethodDefinition method, ReportMethods flow) {
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
                    InsertBefore(il, instruction, il.Create(OpCodes.Call, flow.ReportLineStart));
                    i += 2;
                    lastLine = sequencePoint.StartLine;
                }

                var localIndex = GetIndexIfStloc(instruction);
                if (localIndex == null)
                    continue;

                var variable = il.Body.Variables[localIndex.Value];
                if (string.IsNullOrEmpty(variable.Name))
                    continue;

                var insertTarget = instruction;
                InsertAfter(il, ref insertTarget, ref i, il.Create(OpCodes.Ldstr, variable.Name));
                InsertAfter(il, ref insertTarget, ref i, il.Create(OpCodes.Ldloc, localIndex.Value));
                InsertAfter(il, ref insertTarget, ref i, il.Create(OpCodes.Call, new GenericInstanceMethod(flow.ReportVariable) {
                    GenericArguments = { variable.VariableType }
                }));
            }

            foreach (var handler in il.Body.ExceptionHandlers) {
                if (handler.HandlerType != ExceptionHandlerType.Catch)
                    continue;
                var start = handler.HandlerStart;
                InsertBefore(il, start, il.Create(OpCodes.Dup));
                InsertBefore(il, start, il.Create(OpCodes.Call, flow.ReportException));
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

        private struct ReportMethods {
            public MethodReference ReportLineStart { get; set; }
            public MethodReference ReportVariable { get; set; }
            public MethodReference ReportException { get; set; }
        }
    }
}
