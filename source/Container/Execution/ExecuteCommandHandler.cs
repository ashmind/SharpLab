using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using SharpLab.Container.Protocol;
using SharpLab.Container.Protocol.Stdin;
using SharpLab.Container.Runtime;
using SharpLab.Runtime.Internal;

namespace SharpLab.Container.Execution {
    internal class ExecuteCommandHandler {
        private static readonly object[] EmptyMainArguments = new object[] { Array.Empty<string>() };
        private readonly FlowWriter _flowWriter;
        private readonly StdoutWriter _stdoutWriter;

        public ExecuteCommandHandler(FlowWriter flowWriter, StdoutWriter stdoutWriter) {
            _flowWriter = flowWriter;
            _stdoutWriter = stdoutWriter;
        }

        public void Execute(ExecuteCommand command) {
            var stopwatch = command.IncludePerformance ? Stopwatch.StartNew() : null;
            _stdoutWriter.WriteOutputStart(command.OutputStartMarker);
            try {
                ExecuteAssembly(command.AssemblyBytes);
                _flowWriter.FlushAndReset();
                if (stopwatch != null) {
                    // TODO: Prettify
                    Console.Out.Write($"PERFORMANCE:");
                    Console.Out.Write($"\n  [VM] CONTAINER: {stopwatch.ElapsedMilliseconds,12}ms");
                }
            }
            catch (Exception ex) {
                try {
                    var exceptionToReport = ex is TargetInvocationException { InnerException: {} inner }
                        ? inner
                        : ex;
                    Output.Write(new SimpleInspection("Exception", exceptionToReport.ToString()));
                    Flow.ReportException(exceptionToReport);
                    _flowWriter.FlushAndReset();
                }
                catch {
                    Console.WriteLine(ex);
                }
            }
            _stdoutWriter.WriteOutputEnd(command.OutputEndMarker);
        }

        private void ExecuteAssembly(byte[] assemblyBytes) {
            var context = new AssemblyLoadContext("ExecutorContext", isCollectible: true);
            try {
                var assembly = context.LoadFromStream(new MemoryStream(assemblyBytes));
                var main = assembly.EntryPoint;
                if (main == null) {
                    Output.WriteWarning("Could not find any code to run (either a Main method or any top level code).");
                    return;
                }

                var args = main.GetParameters().Length > 0 ? EmptyMainArguments : null;
                main.Invoke(null, args);
            }
            finally {
                context.Unload();
            }
        }
    }
}
