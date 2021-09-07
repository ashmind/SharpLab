using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MirrorSharp.Advanced;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common;

namespace SharpLab.Server.Execution.Internal {
    public class ContainerFlowReportingRewriter : IContainerAssemblyRewriter {
        private const int HiddenLine = 0xFEEFEE;

        private static readonly MethodInfo ReportLineStartMethod =
            typeof(ContainerFlow).GetMethod(nameof(ContainerFlow.ReportLineStart))!;
        private static readonly MethodInfo ReportValueMethod =
            typeof(ContainerFlow).GetMethod(nameof(ContainerFlow.ReportValue))!;
        private static readonly MethodInfo ReportRefValueMethod =
            typeof(ContainerFlow).GetMethod(nameof(ContainerFlow.ReportRefValue))!;
        private static readonly MethodInfo ReportSpanValueMethod =
            typeof(ContainerFlow).GetMethod(nameof(ContainerFlow.ReportSpanValue))!;
        private static readonly MethodInfo ReportRefSpanValueMethod =
            typeof(ContainerFlow).GetMethod(nameof(ContainerFlow.ReportRefSpanValue))!;
        private static readonly MethodInfo ReportReadOnlySpanValueMethod =
            typeof(ContainerFlow).GetMethod(nameof(ContainerFlow.ReportReadOnlySpanValue))!;
        private static readonly MethodInfo ReportRefReadOnlySpanValueMethod =
            typeof(ContainerFlow).GetMethod(nameof(ContainerFlow.ReportRefReadOnlySpanValue))!;
        private static readonly MethodInfo ReportExceptionMethod =
            typeof(ContainerFlow).GetMethod(nameof(ContainerFlow.ReportException))!;

        private readonly IReadOnlyDictionary<string, ILanguageAdapter> _languages;

        public ContainerFlowReportingRewriter(IReadOnlyList<ILanguageAdapter> languages) {
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
                    if (instruction.OpCode.FlowControl == FlowControl.Call && IsFlowSuppressing((MethodReference)instruction.Operand))
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
                    if (i == 0)
                        TryInsertReportMethodArguments(il, instruction, sequencePoint, method, flow, session, ref i);

                    il.InsertBeforeAndRetargetAll(instruction, il.CreateLdcI4Best(sequencePoint.StartLine));
                    il.InsertBefore(instruction, il.CreateCall(flow.ReportLineStart));
                    i += 2;
                    lastLine = sequencePoint.StartLine;
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
            il.InsertBeforeAndRetargetAll(instruction, getValue);
            il.InsertBefore(instruction, valueName != null ? il.Create(OpCodes.Ldstr, valueName) : il.Create(OpCodes.Ldnull));
            il.InsertBefore(instruction, il.CreateLdcI4Best(line));

            if (valueType is RequiredModifierType requiredType)
                valueType = requiredType.ElementType; // not the same as GetElementType() which unwraps nested ref-types etc

            var report = PrepareReportValue(valueType, flow.ReportValue, flow.ReportSpanValue, flow.ReportReadOnlySpanValue);
            if (valueType is ByReferenceType byRef)
                report = PrepareReportValue(byRef.ElementType, flow.ReportRefValue, flow.ReportRefSpanValue, flow.ReportRefReadOnlySpanValue);

            il.InsertBefore(instruction, il.CreateCall(report));
            index += 4;
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
            il.InsertBeforeAndRetargetAll(start, il.Create(OpCodes.Dup));
            il.InsertBefore(start, il.CreateCall(flow.ReportException));
        }

        private void RewriteFinally(ExceptionHandler handler, ref int handlerIndex, ILProcessor il, ReportMethods flow) {
            // for try/finally, the only thing we can do is to
            // wrap internals of try into a new try+filter+catch
            var outerTryLeave = handler.TryEnd.Previous;
            if (!outerTryLeave.OpCode.Code.IsLeave()) {
                // in some cases (e.g. exception throw) outer handler does
                // not end with `leave` -- but we do need it once we wrap
                // that throw

                // if the handler is the last thing in the method
                if (handler.HandlerEnd == null)
                {
                    var finalReturn = il.Create(OpCodes.Ret);
                    il.Append(finalReturn);
                    handler.HandlerEnd = finalReturn;
                }

                outerTryLeave = il.Create(OpCodes.Leave, handler.HandlerEnd);
                il.InsertBefore(handler.TryEnd, outerTryLeave);
            }

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

        private struct ReportMethods {
            public MethodReference ReportLineStart { get; set; }
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
