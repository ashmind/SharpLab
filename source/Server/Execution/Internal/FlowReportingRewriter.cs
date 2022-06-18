using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using MirrorSharp.Advanced;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Diagnostics;

namespace SharpLab.Server.Execution.Internal {
    public class FlowReportingRewriter : IAssemblyRewriter {
        private const int HiddenLine = 0xFEEFEE;

        private static readonly MethodInfo ReportLineStartMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportLineStart))!;
        /*private static readonly MethodInfo ReportBeforeJumpUpMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportBeforeJumpUp))!;
        private static readonly MethodInfo ReportBeforeJumpDownMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportBeforeJumpDown))!;*/
        private static readonly MethodInfo ReportMethodStartMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportMethodStart))!;
        private static readonly MethodInfo ReportMethodReturnMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportMethodReturn))!;
        private static readonly MethodInfo ReportValueMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportValue))!;
        private static readonly MethodInfo ReportRefValueMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportRefValue))!;
        private static readonly MethodInfo ReportSpanValueMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportSpanValue))!;
        private static readonly MethodInfo ReportRefSpanValueMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportRefSpanValue))!;
        private static readonly MethodInfo ReportReadOnlySpanValueMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportReadOnlySpanValue))!;
        private static readonly MethodInfo ReportRefReadOnlySpanValueMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportRefReadOnlySpanValue))!;
        private static readonly MethodInfo ReportExceptionMethod =
            typeof(Flow).GetMethod(nameof(Flow.ReportException))!;

        private readonly IReadOnlyDictionary<string, ILanguageAdapter> _languages;

        public FlowReportingRewriter(IReadOnlyList<ILanguageAdapter> languages) {
            _languages = languages.ToDictionary(l => l.LanguageName);
        }

        public void Rewrite(AssemblyDefinition assembly, IWorkSession session) {
            foreach (var module in assembly.Modules) {
                foreach (var type in module.Types) {
                    if (HasFlowSupressingCalls(type))
                        return;
                }
            }

            foreach (var module in assembly.Modules) {
                var flow = new ReportMethods {
                    ReportLineStart = module.ImportReference(ReportLineStartMethod),

                    // ReportBeforeJumpUp = module.ImportReference(ReportBeforeJumpUpMethod),
                    // ReportBeforeJumpDown = module.ImportReference(ReportBeforeJumpDownMethod),
                    ReportMethodStart = module.ImportReference(ReportMethodStartMethod),
                    ReportMethodReturn = module.ImportReference(ReportMethodReturnMethod),

                    ReportValue = module.ImportReference(ReportValueMethod),
                    ReportRefValue = module.ImportReference(ReportRefValueMethod),
                    ReportSpanValue = module.ImportReference(ReportSpanValueMethod),
                    ReportRefSpanValue = module.ImportReference(ReportRefSpanValueMethod),
                    ReportReadOnlySpanValue = module.ImportReference(ReportReadOnlySpanValueMethod),
                    ReportRefReadOnlySpanValue = module.ImportReference(ReportRefReadOnlySpanValueMethod),

                    ReportException = module.ImportReference(ReportExceptionMethod),
                };
                foreach (var type in module.Types) {
                    Rewrite(type, flow, session);
                }
            }
        }

        private bool HasFlowSupressingCalls(TypeDefinition type) {
            foreach (var method in type.Methods) {
                if (!method.HasBody || method.Body.Instructions.Count == 0)
                    continue;
                foreach (var instruction in method.Body.Instructions) {
                    var isFlowSupressing = instruction.OpCode.FlowControl == FlowControl.Call
                        && instruction.Operand is MethodReference m
                        && IsFlowSuppressing(m);

                    if (isFlowSupressing)
                        return true;
                }
            }

            foreach (var nested in type.NestedTypes) {
                if (HasFlowSupressingCalls(nested))
                    return true;
            }

            return false;
        }

        private bool IsFlowSuppressing(MethodReference callee) {
            return callee.Name == nameof(Inspect.Allocations)
                && callee.DeclaringType.Name == nameof(Inspect);
        }

        private void Rewrite(TypeDefinition type, ReportMethods flow, IWorkSession session) {
            foreach (var method in type.Methods) {
                Rewrite(method, flow, session);
            }

            foreach (var nested in type.NestedTypes) {
                Rewrite(nested, flow, session);
            }
        }

        private void Rewrite(MethodDefinition method, ReportMethods flow, IWorkSession session) {
            if (!method.HasBody || method.Body.Instructions.Count == 0)
                return;

            method.Body.SimplifyMacros();

            var il = method.Body.GetILProcessor();
            var instructions = il.Body.Instructions;
            var lastLine = (int?)null;
            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];
                var sequencePoint = method.DebugInformation?.GetSequencePoint(instruction);
                var hasSequencePoint = sequencePoint != null && sequencePoint.StartLine != HiddenLine;
                if (!hasSequencePoint && lastLine == null)
                    continue;

                if (hasSequencePoint && sequencePoint!.StartLine != lastLine) {
                    var isMethodStart = i == 0;
                    if (isMethodStart)
                        TryInsertReportMethodArguments(il, instruction, sequencePoint, method, flow, session, ref i);

                    il.InsertBeforeAndRetargetAll(instruction, il.CreateLdcI4Best(sequencePoint.StartLine));
                    il.InsertBefore(instruction, il.CreateCall(flow.ReportLineStart));
                    i += 2;
                    lastLine = sequencePoint.StartLine;

                    if (isMethodStart) {
                        il.InsertBefore(instruction, il.CreateCall(flow.ReportMethodStart));
                        i += 1;
                    }
                }

                if (instruction.OpCode.FlowControl == FlowControl.Return) {
                    il.InsertBeforeAndRetargetAll(instruction, il.CreateCall(flow.ReportMethodReturn));
                    i += 1;
                }

                var valueOrNull = GetValueToReport(instruction, il, session);
                if (valueOrNull == null)
                    continue;

                var value = valueOrNull.Value;
                InsertReportValue(
                    il, instruction,
                    il.Create(OpCodes.Dup), value.type, value.name,
                    sequencePoint?.StartLine ?? lastLine ?? Flow.UnknownLineNumber,
                    flow, ref i
                );
            }

            RewriteExceptionHandlers(il, flow);

            method.Body.OptimizeMacros();
        }

        private void TryInsertReportMethodArguments(ILProcessor il, Instruction instruction, SequencePoint sequencePoint, MethodDefinition method, ReportMethods flow, IWorkSession session, ref int index) {
            if (!method.HasParameters)
                return;

            var parameterLines = _languages[session.LanguageName]
                .GetMethodParameterLines(session, sequencePoint.StartLine, sequencePoint.StartColumn);

            if (parameterLines.Length == 0)
                return;

            // Note: method parameter lines are unreliable and can potentially return
            // wrong lines if nested method syntax is unrecognized and code matches it
            // to the containing method. That is acceptable, as long as parameter count
            // mismatch does not crash things -> so check length here.
            if (parameterLines.Length != method.Parameters.Count)
                return;

            foreach (var parameter in method.Parameters) {
                if (parameter.IsOut)
                    continue;

                InsertReportValue(
                    il, instruction,
                    il.CreateLdargBest(parameter), parameter.ParameterType, parameter.Name,
                    parameterLines[parameter.Index], flow,
                    ref index
                );
            }
        }

        /*private void TryInsertReportJump(ILProcessor il, Instruction instruction, ReportMethods flow, ref int index) {
            var report = instruction.OpCode.FlowControl switch {
                FlowControl.Call => flow.ReportBeforeCall,
                _ => null
            };
            if (report == null)
                return;

            il.InsertBeforeAndRetargetAll(instruction, il.CreateCall(report));
            index += 1;
        }*/

        private (string name, TypeReference type)? GetValueToReport(Instruction instruction, ILProcessor il, IWorkSession session) {
            var localIndex = GetIndexIfStloc(instruction);
            if (localIndex != null) {
                var variable = il.Body.Variables[localIndex.Value];
                var symbols = il.Body.Method.DebugInformation;
                if (symbols == null || !symbols.TryGetName(variable, out var variableName))
                    return null;

                return (variableName, variable.VariableType);
            }

            if (instruction.OpCode.Code == Code.Ret) {
                if (instruction.Previous?.Previous?.OpCode.Code == Code.Tail)
                    return null;
                var method = il.Body.Method;
                if (method.ReturnsVoid())
                    return null;
                return ("return", method.ReturnType);
            }

            return null;
        }

        private void InsertReportValue(
            ILProcessor il,
            Instruction instruction,
            Instruction getValue,
            TypeReference valueType,
            string valueName,
            int line,
            ReportMethods flow,
            ref int index
        ) {
            var report = PrepareReportValue(valueType, flow);
            if (report == null)
                return;

            il.InsertBeforeAndRetargetAll(instruction, getValue);
            il.InsertBefore(instruction, valueName != null ? il.Create(OpCodes.Ldstr, valueName) : il.Create(OpCodes.Ldnull));
            il.InsertBefore(instruction, il.CreateLdcI4Best(line));
            il.InsertBefore(instruction, il.CreateCall(report));
            index += 4;
        }

        private GenericInstanceMethod? PrepareReportValue(TypeReference valueType, ReportMethods flow) {
            if (valueType.IsPointer || valueType.IsFunctionPointer)
                return null;

            if (valueType is RequiredModifierType requiredType)
                valueType = requiredType.ElementType; // not the same as GetElementType() which unwraps nested ref-types etc

            if (valueType is ByReferenceType byRef)
                return PrepareReportValue(byRef.ElementType, flow.ReportRefValue, flow.ReportRefSpanValue, flow.ReportRefReadOnlySpanValue);

            if (!valueType.IsPrimitive && !valueType.IsGenericParameter) {
                var valueTypeDefinition = valueType.Resolve();
                foreach (var attribute in valueTypeDefinition.CustomAttributes) {
                    // ref structs cannot be reported in a generic way
                    if (attribute.AttributeType is { Name: nameof(IsByRefLikeAttribute), Namespace: "System.Runtime.CompilerServices" })
                        return null;
                }
            }

            return PrepareReportValue(valueType, flow.ReportValue, flow.ReportSpanValue, flow.ReportReadOnlySpanValue);
        }

        private GenericInstanceMethod PrepareReportValue(TypeReference valueType, MethodReference reportAnyNonSpan, MethodReference reportSpan, MethodReference reportReadOnlySpan) {
            if (valueType is GenericInstanceType generic) {
                if (generic.ElementType.FullName == "System.Span`1")
                    return new GenericInstanceMethod(reportSpan) { GenericArguments = { generic.GenericArguments[0] } };
                if (generic.ElementType.FullName == "System.ReadOnlySpan`1")
                    return new GenericInstanceMethod(reportReadOnlySpan) { GenericArguments = { generic.GenericArguments[0] } };
            }

            return new GenericInstanceMethod(reportAnyNonSpan) { GenericArguments = { valueType } };
        }

        private void RewriteExceptionHandlers(ILProcessor il, ReportMethods flow) {
            if (!il.Body.HasExceptionHandlers)
                return;

            var handlers = il.Body.ExceptionHandlers;

            LogILIfEnabled("Initial", il);
            for (var i = handlers.Count - 1; i >= 0; i--) {
                EnsureTryLeave(handlers[i], i, il);
                LogILIfEnabled($"EnsureTryLeave.{i}", il);
            }

            for (var i = 0; i < handlers.Count; i++) {
                var handler = handlers[i];
                switch (handler.HandlerType) {
                    case ExceptionHandlerType.Catch:
                        RewriteCatch(handler.HandlerStart, il, flow);
                        break;

                    case ExceptionHandlerType.Filter:
                        RewriteCatch(handler.FilterStart, il, flow);
                        break;

                    case ExceptionHandlerType.Finally:
                        RewriteFinally(handler, ref i, il, flow);
                        break;
                }
            }
        }

        private void EnsureTryLeave(ExceptionHandler handler, int handlerIndex, ILProcessor il) {
            if (handler.TryEnd.Previous.OpCode.Code.IsLeave())
                return;

            // In some cases (e.g. exception throw) handler does
            // not end with `leave` -- but we do need it once we wrap
            // that throw.

            // If the handler is the last thing in the method.
            if (handler.HandlerEnd == null) {
                var finalReturn = il.Create(OpCodes.Ret);
                il.Append(finalReturn);
                handler.HandlerEnd = finalReturn;
            }

            var leave = il.Create(OpCodes.Leave, handler.HandlerEnd);
            il.InsertBefore(handler.TryEnd, leave);
            for (var i = 0; i < handlerIndex; i++) {
                il.Body.ExceptionHandlers[i].RetargetAll(handler.TryEnd, leave);
            }
        }

        private void RewriteCatch(Instruction start, ILProcessor il, ReportMethods flow) {
            il.InsertBeforeAndRetargetAll(start, il.Create(OpCodes.Dup));
            il.InsertBefore(start, il.CreateCall(flow.ReportException));
        }

        private void RewriteFinally(ExceptionHandler handler, ref int handlerIndex, ILProcessor il, ReportMethods flow) {
            // for try/finally, the only thing we can do is to
            // wrap internals of try into a new try+filter+catch
            var outerTryLeave = handler.TryEnd.Previous;

            var innerTryLeave = il.Create(OpCodes.Leave_S, outerTryLeave);
            var reportCall = il.CreateCall(flow.ReportException);
            var catchHandler = il.Create(OpCodes.Pop);

            il.InsertBeforeAndRetargetAll(outerTryLeave, innerTryLeave);
            il.InsertBefore(outerTryLeave, reportCall);
            il.InsertBefore(outerTryLeave, il.Create(OpCodes.Ldc_I4_0));
            il.InsertBefore(outerTryLeave, il.Create(OpCodes.Endfilter));
            il.InsertBefore(outerTryLeave, catchHandler);
            il.InsertBefore(outerTryLeave, il.Create(OpCodes.Leave_S, outerTryLeave));

            for (var i = 0; i < handlerIndex; i++) {
                il.Body.ExceptionHandlers[i].RetargetAll(outerTryLeave.Next, innerTryLeave.Next);
            }

            il.Body.ExceptionHandlers.Insert(handlerIndex, new ExceptionHandler(ExceptionHandlerType.Filter) {
                TryStart = handler.TryStart,
                TryEnd = reportCall,
                FilterStart = reportCall,
                HandlerStart = catchHandler,
                HandlerEnd = outerTryLeave
            });
            handlerIndex += 1;
        }

        private void InsertAfter(ILProcessor il, ref Instruction target, ref int index, Instruction instruction) {
            il.InsertAfter(target, instruction);
            target = instruction;
            index += 1;
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

        [Conditional("DEBUG")]
        private void LogILIfEnabled(string stepName, ILProcessor il) {
            #if DEBUG
            if (!DiagnosticLog.IsEnabled())
                return;

            var builder = new StringBuilder();
            var indent = 0;
            foreach (var instruction in il.Body.Instructions) {
                foreach (var handler in il.Body.ExceptionHandlers) {
                    if (handler.TryStart == instruction) {
                        builder.Append(new string(' ', indent));
                        builder.AppendLine(".try {");
                        indent += 4;
                    }
                    else if (handler.TryEnd == instruction) {
                        indent -= 4;
                        builder.Append(new string(' ', indent));
                        builder.AppendLine("}");
                    }

                    if (handler.HandlerStart == instruction) {
                        builder.Append(new string(' ', indent));
                        builder.AppendLine(handler.HandlerType switch {
                            ExceptionHandlerType.Catch => ".catch {",
                            ExceptionHandlerType.Finally => ".finally {",
                            ExceptionHandlerType.Filter => ".filter {",
                            ExceptionHandlerType.Fault => ".fault {",
                            _ => throw new()
                        });
                        indent += 4;
                    }
                    else if (handler.HandlerEnd == instruction) {
                        indent -= 4;
                        builder.Append(new string(' ', indent));
                        builder.AppendLine("}");
                    }
                }

                builder.Append(new string(' ', indent));
                builder.AppendLine(instruction.ToString());
            }
            DiagnosticLog.LogText("Flow.IL." + il.Body.Method.Name + "." + stepName, builder.ToString());
            #endif
        }

        private struct ReportMethods {
            public MethodReference ReportLineStart { get; set; }
            // public MethodReference ReportBeforeJumpUp { get; set; }
            // public MethodReference ReportBeforeJumpDown { get; set; }
            public MethodReference ReportMethodStart { get; set; }
            public MethodReference ReportMethodReturn { get; set; }
            public MethodReference ReportValue { get; set; }
            public MethodReference ReportRefValue { get; set; }
            public MethodReference ReportSpanValue { get; set; }
            public MethodReference ReportRefSpanValue { get; set; }
            public MethodReference ReportReadOnlySpanValue { get; set; }
            public MethodReference ReportRefReadOnlySpanValue { get; set; }
            public MethodReference ReportException { get; set; }
        }
    }
}
