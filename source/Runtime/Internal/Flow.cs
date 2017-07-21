using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLab.Runtime.Internal {
    public static class Flow {
        private const int MaxReportLength = 20;
        private const int MaxReportEnumerableItemCount = 3;
        private const int MaxReportStepNotesPerLineCount = 3;
        private const int MaxReportVariablesPerStepCount = 3;

        private static readonly IDictionary<int, int> _stepNotesCountPerLine = new Dictionary<int, int>();
        private static readonly List<Step> _steps = new List<Step>();
        public static IReadOnlyList<Step> Steps => _steps;

        public static void ReportLineStart(int lineNumber) {
            _steps.Add(new Step(lineNumber));
        }

        public static void ReportVariable<T>(string name, T value) {
            var step = _steps[_steps.Count - 1];
            if (!_stepNotesCountPerLine.TryGetValue(step.LineNumber, out int countPerLine))
                countPerLine = 0;

            if (step.Notes == null) {
                countPerLine += 1;
                _stepNotesCountPerLine[step.LineNumber] = countPerLine;

                if (countPerLine == MaxReportStepNotesPerLineCount + 1) {
                    step.Notes = new StringBuilder("…");
                    _steps[_steps.Count - 1] = step;
                    return;
                }
            }

            if (countPerLine >= MaxReportStepNotesPerLineCount + 1)
                return;

            step.VariableCount += 1;
            if (step.VariableCount > MaxReportVariablesPerStepCount + 1)
                return;

            var notes = step.Notes;
            if (notes == null) {
                notes = new StringBuilder();
                step.Notes = notes;
            }

            if (notes.Length > 0)
                notes.Append(", ");

            if (step.VariableCount == MaxReportVariablesPerStepCount + 1) {
                notes.Append("…");
                return;
            }

            notes.Append(name).Append(": ");
            AppendValue(notes, value);
            // Have to reassign in case we set Notes
            _steps[_steps.Count - 1] = step;
        }

        public static void ReportException(object exception) {
            var step = _steps[_steps.Count - 1];
            step.Exception = exception;
            _steps[_steps.Count - 1] = step;
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

                if (index > MaxReportEnumerableItemCount) {
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
        public struct Step {
            public Step(int lineNumber) {
                LineNumber = lineNumber;
                Notes = null;
                Exception = null;
                VariableCount = 0;
            }

            public int LineNumber { get; }
            public object Exception { get; internal set; }
            public StringBuilder Notes { get; internal set; }

            internal int VariableCount { get; set; }
        }
    }
}
