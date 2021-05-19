using System;

namespace SharpLab.Container.Protocol {
    public class StdoutProtocol {
        public const string EndOutput = "END OUTPUT";

        public void WriteEndOutput(string id) {
            Console.Write(EndOutput);
            Console.Write(" ");
            Console.Write(id);
        }
    }
}
