using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpLab.Runtime.Internal {
    public static class Flow {
        private const int MaxReportLength = 20;
        private const int MaxReportItemCount = 3;

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

        public static void ReportException(Exception exception) {
            var line = _lines[_lines.Count - 1];
            line.Exception = exception;
            _lines[_lines.Count - 1] = line;
        }

        private static bool HadException() {
            return Marshal.GetExceptionPointers() != IntPtr.Zero || Marshal.GetExceptionCode() != 0;
        }

        private static StringBuilder AppendValue<T>(StringBuilder builder, T value) {
            if (value == null)
                return builder.Append("null");

            switch (value) {
                case IList<int> e: return AppendEnumerable(e, builder);
                case ICollection e: return AppendEnumerable(e.Cast<object>(), builder);
                default: return AppendString(value.ToString(), builder);
            }
        }

        private static StringBuilder AppendEnumerable<T>(IEnumerable<T> enumerable, StringBuilder builder) {
            builder.Append("{ ");
            var index = 0;
            foreach (var item in enumerable) {
                if (index > 0)
                    builder.Append(", ");

                if (index > MaxReportItemCount) {
                    builder.Append("…");
                    break;
                }

                AppendValue(builder, item);
                index += 1;
            }
            builder.Append(" }");
            return builder;
        }

        private static StringBuilder AppendString(string value, StringBuilder builder) {
            var limit = MaxReportLength - builder.Length;
            if (limit <= 0)
                return builder;

            if (value.Length <= limit) {
                builder.Append(value);
            }
            else {
                builder.Append(value, 0, limit);
                builder.Append("…");
            }

            return builder;
        }

        [Serializable]
        public struct Line {
            private StringBuilder _notes;

            public Line(int number) {
                Number = number;
                _notes = null;
                Exception = null;
            }

            public int Number { get; }
            public Exception Exception { get; internal set; }

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
