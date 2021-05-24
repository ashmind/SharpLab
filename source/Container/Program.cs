using System;
using System.Diagnostics;
using System.IO;
using ProtoBuf;
using SharpLab.Container.Execution;
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

        public static void SafeMain() {
            using var input = Console.OpenStandardInput(1024);
            using var output = Console.OpenStandardOutput(1024);

            Console.WriteLine("START");
            RuntimeServices.InspectionWriter = new InspectionWriter(output);
            RuntimeServices.ValuePresenter = new ValuePresenter();

            var shouldExit = false;
            while (!shouldExit) {
                Console.WriteLine("READ COMMAND");
                var command = Serializer.DeserializeWithLengthPrefix<ExecuteCommand?>(input, PrefixStyle.Base128);
                if (command == null)
                    break; // end-of-input
                HandleExecuteCommand(command, ref shouldExit);
            }

            Console.WriteLine("END");
        }

        private static void HandleExecuteCommand(ExecuteCommand command, ref bool shouldExit) {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine("EXECUTE");
            _executor.Execute(new MemoryStream(command.AssemblyBytes));
            Console.Out.Write($"PERFORMANCE:");
            Console.Out.Write($"\n  [VM] CONTAINER: {stopwatch.ElapsedMilliseconds,12}ms");
            Console.Out.Write(command.OutputEndMarker);                
            Console.Out.Flush();
            return;
        }
    }
}
