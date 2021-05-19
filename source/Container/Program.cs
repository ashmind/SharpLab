using System;
using System.IO;
using ProtoBuf;
using SharpLab.Container.Internal;
using SharpLab.Container.Protocol.Stdin;

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

            Console.WriteLine("START");

            var shouldExit = false;
            while (!shouldExit) {
                var command = Serializer.DeserializeWithLengthPrefix<StdinCommand>(input, PrefixStyle.Base128);
                HandleCommand(command, ref shouldExit);
            }

            Console.WriteLine("END");
        }

        private static void HandleCommand(StdinCommand command, ref bool shouldExit) {
            if (command is ExecuteCommand execute) {
                Console.WriteLine("EXECUTE");
                _executor.Execute(new MemoryStream(execute.AssemblyBytes));
                Console.Out.Write(execute.OutputEndMarker);
                Console.Out.Flush();
                return;
            }

            if (command is ExitCommand) {
                Console.WriteLine("EXIT");
                shouldExit = true;
                return;
            }
        }
    }
}
