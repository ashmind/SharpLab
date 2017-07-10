using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLab.Runtime.Internal {
    public static class Flow {
        private static Dictionary<int, Line> _lines = new Dictionary<int, Line>();
        public static IReadOnlyDictionary<int, Line> Lines => _lines;

        private static int _visitSequence = 0;

        public static void ReportVariable<T>(string name, T value, int lineNumber) {
            var notes = GetLine(lineNumber).Notes;
            if (notes.Length > 0)
                notes.Append(", ");
            notes.Append(name).Append(": ");
            AppendValue(notes, value);
        }

        public static void ReportLineStart(int lineNumber) {
            _visitSequence += 1;
            GetLine(lineNumber).Visit(_visitSequence);
        }

        private static Line GetLine(int number) {
            if (!_lines.TryGetValue(number, out var line)) {
                line = new Line();
                _lines.Add(number, line);
            }
            return line;
        }

        private static StringBuilder AppendValue<T>(StringBuilder builder, T value) {
            if (value == null)
                return builder.Append("null");

            switch (value) {
                case IList<int> e: return AppendEnumerable(e, builder);
                case ICollection e: return AppendEnumerable(e.Cast<object>(), builder);
                default: return builder.Append(value);
            }
        }

        private static StringBuilder AppendEnumerable<T>(IEnumerable<T> enumerable, StringBuilder builder) {
            builder.Append("{ ");
            var first = true;
            foreach (var item in enumerable) {
                if (!first) {
                    builder.Append(", ");
                }
                else {
                    first = false;
                }

                AppendValue(builder, item);
            }
            builder.Append(" }");
            return builder;
        }

        [Serializable]
        public class Line {
            private StringBuilder _notes;

            public bool HasNotes => _notes != null;
            public StringBuilder Notes {
                get {
                    if (_notes == null)
                        _notes = new StringBuilder();
                    return _notes;
                }
            }

            public void Visit(int sequence) {
                if (SingleVisit == null && MultipleVisits == null) {
                    SingleVisit = sequence;
                    return;
                }

                var multipleVisits = (List<int>)MultipleVisits;
                if (multipleVisits == null) {
                    multipleVisits = new List<int>();
                    MultipleVisits = multipleVisits;
                }
                if (SingleVisit != null) {
                    multipleVisits.Add(SingleVisit.Value);
                    SingleVisit = null;
                }
                multipleVisits.Add(sequence);             
            }

            public int? SingleVisit { get; private set; }
            public IReadOnlyList<int> MultipleVisits { get; private set; }
        }
    }
}
