using System;
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
            var stdoutWriter = new StdoutWriter(stdout, new Utf8JsonWriter(stdout, new() {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));

            var flowWriter = new FlowWriter(stdoutWriter, new Utf8ValuePresenter());
            SetupRuntimeServices(flowWriter, stdoutWriter);

            var executeCommandHandler = new ExecuteCommandHandler(flowWriter, stdoutWriter);
            var shouldExit = false;
            while (!shouldExit) {
                var command = Serializer.DeserializeWithLengthPrefix<ExecuteCommand?>(stdin, PrefixStyle.Base128);
                if (command == null)
                    break; // end-of-input
                executeCommandHandler.Execute(command);
            }
        }

        private static void SetupRuntimeServices(FlowWriter flowWriter, StdoutWriter stdoutWriter) {
            #pragma warning disable CS0618 // Type or member is obsolete
            RuntimeServices.ValuePresenter = new LegacyValuePresenter();
            #pragma warning restore CS0618 // Type or member is obsolete
            RuntimeServices.InspectionWriter = new InspectionWriter(stdoutWriter);
            RuntimeServices.FlowWriter = flowWriter;
            RuntimeServices.MemoryBytesInspector = new MemoryBytesInspector(new Pool<ClrRuntime>(() => {
                var dataTarget = DataTarget.AttachToProcess(Current.ProcessId, uint.MaxValue, AttachFlag.Passive);
                return dataTarget.ClrVersions.Single(c => c.Flavor == ClrFlavor.Core).CreateRuntime();
            }));
            RuntimeServices.MemoryGraphBuilderFactory = argumentNames => new MemoryGraphBuilder(argumentNames, RuntimeServices.ValuePresenter);
        }
    }
}
