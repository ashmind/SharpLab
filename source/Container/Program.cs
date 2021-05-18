using System;
using System.IO;
using System.Text;
using SharpLab.Container.Internal;

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

        public static void SafeMain() {
            using var input = Console.OpenStandardInput(1024);

            Console.WriteLine("START");

            var executor = new Executor();
            var line = ReadLine(input);
            while (line != null) {
                Console.WriteLine("READLINE");
                if (line.StartsWith("EXECUTE")) {
                    Console.WriteLine("EXECUTE");
                    HandleExecute(input, line, executor);
                }
                line = null;
                //line = Console.ReadLine();
            }
            Console.WriteLine("END");
        }

        private static ReadOnlySpan<char> ReadLine(Stream input) {
            var length = 0;
            var chars = new char[100];
            while (true) {
                var @byte = input.ReadByte();
                if (@byte is -1 or (byte)'\n')
                    break;
                chars[length] = (char)@byte;
                length += 1;
            }
            return new ReadOnlySpan<char>(chars, 0, length);
        }

        private static void HandleExecute(Stream input, ReadOnlySpan<char> line, Executor executor) {
            var length = int.Parse(line.Slice(line.IndexOf(":") + 1));
            Console.WriteLine("LENGTH:" + length);

            var bytes = new byte[length];

            var offset = 0;
            while (offset < length) {
                Console.WriteLine("OFFSET:" + offset);                
                offset += input.Read(bytes, offset, length - offset);
            }            

            executor.Execute(new MemoryStream(bytes));
        }
    }
}
