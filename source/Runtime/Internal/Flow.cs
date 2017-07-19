using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLab.Runtime.Internal {
    public static class Flow {
        private static readonly List<Line> _lines = new List<Line>();
        public static IReadOnlyList<Line> Lines => _lines;

        public static void ReportLineStart(int lineNumber) {
            _lines.Add(new Line(lineNumber));
        }

        public static void ReportVariable<T>(string name, T value) {
            var line = _lines[_lines.Count - 1];
            var notes = line.Notes;
            if (notes.Length > 0)
                notes.Append(", ");
            notes.Append(name).Append(": ");
            AppendValue(notes, value);
            _lines[_lines.Count - 1] = line;
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
        public struct Line {
            private StringBuilder _notes;

            public Line(int number) {
                _notes = null;
                Number = number;
            }

            public int Number { get; }

            public bool HasNotes => _notes != null;
            public StringBuilder Notes {
                get {
                    if (_notes == null)
                        _notes = new StringBuilder();
                    return _notes;
                }
            }            
        }
    }
}
