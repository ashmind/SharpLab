using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Diagnostics.Runtime;
using ProtoBuf;
using SharpLab.Container.Execution;
using SharpLab.Container.Protocol;
using SharpLab.Container.Protocol.Stdin;
using SharpLab.Container.Runtime;
using SharpLab.Runtime.Internal;

namespace SharpLab.Container {
    public static class Program {
        private static readonly Executor _executor = new();

        public static void Main() {
            try {
                SafeMain();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void SafeMain() {
            using var stdin = Console.OpenStandardInput(1024);
            using var stdout = Console.OpenStandardOutput(1024);

            Run(stdin, stdout);
        }

        // TODO: Change test structure so that this can be inlined
        internal static void Run(Stream stdin, Stream stdout) {
            var stdoutWriter = new StdoutJsonLineWriter(stdout, new Utf8JsonWriter(stdout, new() {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));
            SetupRuntimeServices(stdoutWriter);

            var shouldExit = false;
            while (!shouldExit) {
                var command = Serializer.DeserializeWithLengthPrefix<ExecuteCommand?>(stdin, PrefixStyle.Base128);
                if (command == null)
                    break; // end-of-input
                HandleExecuteCommand(command);
            }
        }

        private static void SetupRuntimeServices(StdoutJsonLineWriter writer) {
            RuntimeServices.ValuePresenter = new ValuePresenter();
            RuntimeServices.InspectionWriter = new InspectionWriter(writer);
            RuntimeServices.FlowWriter = new FlowWriter(writer, new ContainerUtf8ValuePresenter());
            RuntimeServices.MemoryBytesInspector = new MemoryBytesInspector(new Pool<ClrRuntime>(() => {
                var dataTarget = DataTarget.AttachToProcess(Current.ProcessId, uint.MaxValue, AttachFlag.Passive);
                return dataTarget.ClrVersions.Single(c => c.Flavor == ClrFlavor.Core).CreateRuntime();
            }));
            RuntimeServices.MemoryGraphBuilderFactory = argumentNames => new MemoryGraphBuilder(argumentNames, RuntimeServices.ValuePresenter);
        }

        private static void HandleExecuteCommand(ExecuteCommand command) {
            var stopwatch = command.IncludePerformance ? Stopwatch.StartNew() : null;
            _executor.Execute(new MemoryStream(command.AssemblyBytes));
            ((FlowWriter)RuntimeServices.FlowWriter).FlushAndReset();
            if (stopwatch != null) {
                // TODO: Prettify
                Console.Out.Write($"PERFORMANCE:");
                Console.Out.Write($"\n  [VM] CONTAINER: {stopwatch.ElapsedMilliseconds,12}ms");
            }
            Console.Out.Write(command.OutputEndMarker);
            Console.Out.Flush();
            return;
        }
    }
}
