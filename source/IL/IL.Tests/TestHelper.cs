using System.IO;
using IL.Syntax;

namespace IL.Tests {
    public static class TestHelper {
        public static CompilationUnitNode Parse(string code) {
            return new ILParser().Parse(new StringReader(code));
        }
    }
}
