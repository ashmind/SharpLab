using System.IO;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace SharpLab.Server.Decompilation.Internal {
    public class SpaceIndentingPlainTextOutput : ITextOutput {
        private const string IndentString = "    ";

        private readonly TextWriter _writer;

        private int _indentLevel;
        private bool _indentRequired;

        private int _line = 1;
        private int _column = 1;

        public SpaceIndentingPlainTextOutput(TextWriter writer)
        {
            Argument.NotNull(nameof(writer), writer);
            _writer = writer;
        }

        private int IndentSize => _indentLevel * IndentString.Length;

        public override string? ToString() {
            return _writer.ToString();
        }

        public void Indent() {
            _indentLevel += 1;
        }

        public void Unindent() {
            _indentLevel -= 1;
        }

        public void Write(char ch) {
            WriteIndentIfRequired();
            _writer.Write(ch);
            _column += 1;
        }

        public void Write(string text) {
            WriteIndentIfRequired();
            _writer.Write(text);
            _column += text.Length;
        }

        public void WriteLine() {
            _writer.WriteLine();
            _indentRequired = true;
            _line += 1;
            _column = 1;
        }

        private void WriteIndentIfRequired()
        {
            if (!_indentRequired)
                return;

            for (var i = 0; i < _indentLevel; i++) {
                _writer.Write(IndentString);
            }
            _column += IndentSize;
            _indentRequired = false;
        }

        public void WriteLocalReference(string text, object reference, bool isDefinition = false) {
            Write(text);
        }

        public void WriteReference(OpCodeInfo opCode) {
            Write(opCode.Name);
        }

        public void WriteReference(PEFile module, EntityHandle handle, string text, bool isDefinition = false) {
            Write(text);
        }

        public void WriteReference(IType type, string text, bool isDefinition = false) {
            Write(text);
        }

        public void WriteReference(IMember member, string text, bool isDefinition = false) {
            Write(text);
        }

        void ITextOutput.MarkFoldStart(string collapsedText, bool defaultCollapsed) { }
        void ITextOutput.MarkFoldEnd() { }
    }
}