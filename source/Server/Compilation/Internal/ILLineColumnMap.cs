using System.Collections.Generic;
using System.Text;

namespace SharpLab.Server.Compilation.Internal {
    // Same as one inside FSharp, might want to consolidate later
    public class ILLineColumnMap {
        private readonly IReadOnlyList<Line> _map;

        private ILLineColumnMap(IReadOnlyList<Line> map, int textLength) {
            _map = map;
            TextLength = textLength;
        }

        public int GetOffset(int line, int column) {
            if (line < 1)
                return column;

            // slightly weird behaviour in some AST ranges
            if (line == _map.Count + 1 && column == 0)
                return _map[line - 2].End;

            return _map[line - 1].Start + column;
        }

        public int TextLength { get; }

        public static ILLineColumnMap BuildFor(StringBuilder stringBuilderForReadOnly) {
            var map = new List<Line>();
            var start = 0;
            var previous = '\0';
            var offset = 0;
            foreach (var chunk in stringBuilderForReadOnly.GetChunks()) {                
                for (var i = 0; i < chunk.Length; i++) {
                    var @char = chunk.Span[i];
                    if (@char == '\r' || (previous != '\r' && @char == '\n'))
                        map.Add(new Line(map.Count + 1, start, offset + i));
                    if (previous == '\n' || (previous == '\r' && @char != '\n'))
                        start = offset + i;
                    previous = @char;
                }
                offset += chunk.Length;
            }
            map.Add(new Line(map.Count + 1, start, stringBuilderForReadOnly.Length));
            return new ILLineColumnMap(map, stringBuilderForReadOnly.Length);
        }

        public struct Line {
            public Line(int number, int start, int end) {
                Number = number;
                Start = start;
                End = end;
            }

            public int Number { get; }
            public int Start { get; }
            public int End { get; }
            public int Length => End - Start;
        }
    }
}
