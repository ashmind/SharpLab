using System;
using System.Reflection;
using MirrorSharp.Advanced;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SharpLab.Runtime.Internal;

namespace SharpLab.Server.Execution.Internal {
    public class FlowReportingRewriter : IAssemblyRewriter {
        private const int HiddenLine = 0xFEEFEE;

        private static readonly MethodInfo ReportLineStartMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportLineStart));
        private static readonly MethodInfo ReportValueMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportValue));
        private static readonly MethodInfo ReportExceptionMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportException));

        public void Rewrite(AssemblyDefinition assembly, IWorkSession session) {
            foreach (var module in assembly.Modules) {
                var flow = new ReportMethods {
                    ReportLineStart = module.ImportReference(ReportLineStartMethod),
                    ReportValue = module.ImportReference(ReportValueMethod),
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
                if (sequencePoint != null && sequencePoint.StartLine != HiddenLine && sequencePoint.StartLine != lastLine) {
                    InsertBeforeAndRetargetAll(il, instruction, il.CreateLdcI4Best(sequencePoint.StartLine));
                    il.InsertBefore(instruction, il.CreateCall(flow.ReportLineStart));
                    i += 2;
                    lastLine = sequencePoint.StartLine;
                }

                var value = GetValueToReport(instruction, il);
                if (value.name == null)
                    continue;

                var insertTarget = instruction;
                il.InsertBefore(instruction, il.Create(OpCodes.Dup));
                il.InsertBefore(instruction, il.Create(OpCodes.Ldstr, value.name));
                il.InsertBefore(instruction, il.CreateLdcI4Best(sequencePoint?.StartLine ?? lastLine ?? Flow.UnknownLineNumber));
                il.InsertBefore(instruction, il.CreateCall(new GenericInstanceMethod(flow.ReportValue) {
                    GenericArguments = { value.type }
                }));
                i += 4;
            }

            RewriteExceptionHandlers(il, flow);
        }

        private (string name, TypeReference type) GetValueToReport(Instruction instruction, ILProcessor il) {
            var localIndex = GetIndexIfStloc(instruction);
            if (localIndex != null) {
                var variable = il.Body.Variables[localIndex.Value];
                if (string.IsNullOrEmpty(variable.Name))
                    return (null, null);

                return (variable.Name, variable.VariableType);
            }

            var code = instruction.OpCode.Code;
            if (code == Code.Stsfld || code == Code.Stfld) {
                var field = (FieldReference)instruction.Operand;
                return (field.Name, field.FieldType);
            }

            return (null, null);
        }

        private void RewriteExceptionHandlers(ILProcessor il, ReportMethods flow) {
            if (!il.Body.HasExceptionHandlers)
                return;

            var handlers = il.Body.ExceptionHandlers;
            for (var i = 0; i < handlers.Count; i++) {
                switch (handlers[i].HandlerType) {
                    case ExceptionHandlerType.Catch:
                        RewriteCatch(handlers[i].HandlerStart, il, flow);
                        break;

                    case ExceptionHandlerType.Filter:
                        RewriteCatch(handlers[i].FilterStart, il, flow);
                        break;

                    case ExceptionHandlerType.Finally:
                        RewriteFinally(handlers[i], ref i, il, flow);
                        break;
                }
            }
        }

        private void RewriteCatch(Instruction start, ILProcessor il, ReportMethods flow) {
            InsertBeforeAndRetargetAll(il, start, il.Create(OpCodes.Dup));
            il.InsertBefore(start, il.CreateCall(flow.ReportException));
        }

        private void RewriteFinally(ExceptionHandler handler, ref int handlerIndex, ILProcessor il, ReportMethods flow) {
            var oldTryLeave = handler.TryEnd.Previous;

            var newTryLeave = il.Create(OpCodes.Leave_S, (Instruction)oldTryLeave.Operand);
            var reportCall = il.CreateCall(flow.ReportException);
            var catchHandler = il.Create(OpCodes.Pop);

            InsertBeforeAndRetargetAll(il, oldTryLeave, newTryLeave);
            il.InsertBefore(oldTryLeave, reportCall);
            il.InsertBefore(oldTryLeave, il.Create(OpCodes.Ldc_I4_0));
            il.InsertBefore(oldTryLeave, il.Create(OpCodes.Endfilter));
            il.InsertBefore(oldTryLeave, catchHandler);
            
            for (var i = 0; i < handlerIndex; i++) {
                il.Body.ExceptionHandlers[i].RetargetAll(oldTryLeave.Next, newTryLeave.Next);
            }

            il.Body.ExceptionHandlers.Insert(handlerIndex, new ExceptionHandler(ExceptionHandlerType.Filter) {
                TryStart = handler.TryStart,
                TryEnd = reportCall,
                FilterStart = reportCall,
                HandlerStart = catchHandler,
                HandlerEnd = oldTryLeave.Next
            });
            handlerIndex += 1;
        }

        private void InsertAfter(ILProcessor il, ref Instruction target, ref int index, Instruction instruction) {
            il.InsertAfter(target, instruction);
            target = instruction;
            index += 1;
        }

        private void InsertBeforeAndRetargetAll(ILProcessor il, Instruction target, Instruction instruction) {
            il.InsertBefore(target, instruction);
            RetargetAll(il, target, instruction);
        }

        private static void RetargetAll(ILProcessor il, Instruction from, Instruction to) {
            foreach (var other in il.Body.Instructions) {
                if (other.Operand == from)
                    other.Operand = to;
            }

            if (!il.Body.HasExceptionHandlers)
                return;

            foreach (var handler in il.Body.ExceptionHandlers) {
                handler.RetargetAll(from, to);
            }
        }

        private int? GetIndexIfStloc(Instruction instruction) {
            switch (instruction.OpCode.Code) {
                case Code.Stloc_0: return 0;
                case Code.Stloc_1: return 1;
                case Code.Stloc_2: return 2;
                case Code.Stloc_3: return 3;

                case Code.Stloc_S:
                case Code.Stloc:
                    return ((VariableReference)instruction.Operand).Index;

                default: return null;
            }
        }

        private struct ReportMethods {
            public MethodReference ReportLineStart { get; set; }
            public MethodReference ReportValue { get; set; }
            public MethodReference ReportException { get; set; }
        }
    }
}
