using System.Collections.Generic;

namespace SharpLab.Server.Compilation.Internal {
    // Same as one inside FSharp, might want to consolidate later
    internal class ILLineColumnMap {
        private readonly IReadOnlyList<Line> _map;

        private ILLineColumnMap(IReadOnlyList<Line> map) {
            _map = map;
        }

        public (Line line, int column) GetLineAndColumn(int offset) {
            var map = _map;
            var line = map[0];
            for (var i = 1; i < map.Count; i++) {
                var nextLine = map[i];
                if (offset < nextLine.Start)
                    break;
                line = nextLine;
            }
            return (line, offset - line.Start);
        }

        public int GetOffset(int line, int column) {
            if (line < 1)
                return column;

            // slightly weird behaviour in some AST ranges
            if (line == _map.Count + 1 && column == 0)
                return _map[line - 2].End;

            return _map[line - 1].Start + column;
        }

        public static ILLineColumnMap BuildFor(string text) {
            var map = new List<Line>();
            var start = 0;
            var previous = '\0';
            for (var i = 0; i < text.Length; i++) {
                var @char = text[i];
                if (@char == '\r' || (previous != '\r' && @char == '\n'))
                    map.Add(new Line(map.Count + 1, start, i));
                if (previous == '\n' || (previous == '\r' && @char != '\n'))
                    start = i;
                previous = @char;
            }
            map.Add(new Line(map.Count + 1, start, text.Length));
            return new ILLineColumnMap(map);
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
