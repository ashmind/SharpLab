using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MirrorSharp.Advanced;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common;

namespace SharpLab.Server.Execution.Internal {
    public class FlowReportingRewriter : IAssemblyRewriter {
        private const int HiddenLine = 0xFEEFEE;

        private static readonly MethodInfo ReportLineStartMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportLineStart));
        private static readonly MethodInfo ReportValueMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportValue));
        private static readonly MethodInfo ReportExceptionMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportException));

        private readonly IReadOnlyDictionary<string, ILanguageAdapter> _languages;

        public FlowReportingRewriter(IReadOnlyList<ILanguageAdapter> languages) {
            _languages = languages.ToDictionary(l => l.LanguageName);
        }

        public void Rewrite(AssemblyDefinition assembly, IWorkSession session) {
            foreach (var module in assembly.Modules) {
                var flow = new ReportMethods {
                    ReportLineStart = module.ImportReference(ReportLineStartMethod),
                    ReportValue = module.ImportReference(ReportValueMethod),
                    ReportException = module.ImportReference(ReportExceptionMethod),
                };
                foreach (var type in module.Types) {
                    Rewrite(type, flow, session);
                    foreach (var nested in type.NestedTypes) {
                        Rewrite(nested, flow, session);
                    }
                }
            }
        }

        private void Rewrite(TypeDefinition type, ReportMethods flow, IWorkSession session) {
            foreach (var method in type.Methods) {
                Rewrite(method, flow, session);
            }
        }

        private void Rewrite(MethodDefinition method, ReportMethods flow, IWorkSession session) {
            if (!method.HasBody || method.Body.Instructions.Count == 0)
                return;

            var il = method.Body.GetILProcessor();
            var instructions = il.Body.Instructions;
            var lastLine = (int?)null;
            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];
                var sequencePoint = instruction.SequencePoint;
                var hasSequencePoint = sequencePoint != null && sequencePoint.StartLine != HiddenLine;
                if (!hasSequencePoint && lastLine == null)
                    continue;

                if (hasSequencePoint && sequencePoint.StartLine != lastLine) {
                    if (i == 0)
                        TryInsertReportMethodArguments(il, instruction, method, flow, session);

                    InsertBeforeAndRetargetAll(il, instruction, il.CreateLdcI4Best(sequencePoint.StartLine));
                    il.InsertBefore(instruction, il.CreateCall(flow.ReportLineStart));
                    i += 2;
                    lastLine = sequencePoint.StartLine;
                }

                var valueOrNull = GetValueToReport(instruction, il);
                if (valueOrNull == null)
                    continue;

                var value = valueOrNull.Value;
                InsertReportValue(
                    il, instruction,
                    il.Create(OpCodes.Dup), value.type, value.name,
                    sequencePoint?.StartLine ?? lastLine ?? Flow.UnknownLineNumber,
                    flow
                );
                i += 4;
            }

            RewriteExceptionHandlers(il, flow);
        }

        private void TryInsertReportMethodArguments(ILProcessor il, Instruction instruction, MethodDefinition method, ReportMethods flow, IWorkSession session) {
            if (!method.HasParameters)
                return;

            var sequencePoint = instruction.SequencePoint;
            var parameterLines = _languages[session.LanguageName]
                .GetMethodParameterLines(session, sequencePoint.StartLine, sequencePoint.StartColumn);

            if (parameterLines.Length == 0)
                return;

            foreach (var parameter in method.Parameters) {
                if (parameter.ParameterType.IsByReference)
                    continue;
                InsertReportValue(
                    il, instruction,
                    il.CreateLdargBest(parameter), parameter.ParameterType, parameter.Name,
                    parameterLines[parameter.Index], flow
                );
            }
        }

        private (string name, TypeReference type)? GetValueToReport(Instruction instruction, ILProcessor il) {
            var localIndex = GetIndexIfStloc(instruction);
            if (localIndex != null) {
                var variable = il.Body.Variables[localIndex.Value];
                if (string.IsNullOrEmpty(variable.Name))
                    return null;

                return (variable.Name, variable.VariableType);
            }

            var code = instruction.OpCode.Code;
            switch (code) {
                //case Code.Stfld:
                //case Code.Stsfld:
                    //var field = (FieldReference)instruction.Operand;
                    //return (field.Name, field.FieldType);

                case Code.Ret:
                    if (instruction.Previous?.Previous?.OpCode.Code == Code.Tail)
                        return null;
                    var returnType = il.Body.Method.ReturnType;
                    if (returnType.IsVoid())
                        return null;
                    return (null, returnType);

                default:
                    return null;
            }
        }

        private void InsertReportValue(ILProcessor il, Instruction instruction, Instruction getValue, TypeReference valueType, string valueName, int line, ReportMethods flow) {
            il.InsertBefore(instruction, getValue);
            il.InsertBefore(instruction, valueName != null ? il.Create(OpCodes.Ldstr, valueName) : il.Create(OpCodes.Ldnull));
            il.InsertBefore(instruction, il.CreateLdcI4Best(line));
            il.InsertBefore(instruction, il.CreateCall(new GenericInstanceMethod(flow.ReportValue) {
                GenericArguments = { valueType }
            }));
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
