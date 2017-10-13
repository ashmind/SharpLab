using System.IO;
using IL.Syntax;
using Pidgin;

namespace IL {
    public class ILParser {
        public CompilationUnitNode Parse(TextReader source) {
            return ILGrammar.Root.ParseOrThrow(source);
        }
    }
}
