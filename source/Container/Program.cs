using System;
using System.IO;
using ProtoBuf;
using SharpLab.Container.Internal;
using SharpLab.Container.Protocol;
using SharpLab.Container.Protocol.Stdin;

namespace SharpLab.Container {
    public static class Program {
        private static readonly Executor _executor = new();
        private static readonly StdoutProtocol _stdoutProtocol = new();

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
                _stdoutProtocol.WriteEndOutput(execute.OutputId);
                return;
            }

            if (command is ExitCommand exit) {
                Console.WriteLine("EXIT");
                shouldExit = true;
                return;
            }
        }
    }
}
