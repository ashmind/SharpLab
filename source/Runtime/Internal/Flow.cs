using System;
using System.Collections.Generic;
using System.Text;

namespace SharpLab.Runtime.Internal {
    public static class Flow {
        public const int UnknownLineNumber = -1;

        private static class ReportLimits {
            public const int MaxStepCount = 50;
            public const int MaxStepNotesPerLine = 3;
            public const int MaxValuesPerStep = 3;

            public static readonly ValuePresenterLimits ValueName = new ValuePresenterLimits(maxValueLength: 10);
            public static readonly ValuePresenterLimits ValueValue = new ValuePresenterLimits(
                maxDepth: 2, maxEnumerableItemCount: 3, maxValueLength: 10
            );
        }

        private static readonly IDictionary<int, int> _stepNotesCountPerLine = new Dictionary<int, int>();
        private static readonly IList<Step> _steps = new List<Step>();

        public static IReadOnlyList<Step> Steps => (IReadOnlyList<Step>)_steps;

        public static void ReportLineStart(int lineNumber) {
            if (_steps.Count > 0) {
                var lastStep = _steps[_steps.Count - 1];
                if (lastStep.LineNumber == lineNumber & lastStep.LineSkipped) {
                    lastStep.LineSkipped = false;
                    _steps[_steps.Count - 1] = lastStep;
                    return;
                }
            }
            if (_steps.Count >= ReportLimits.MaxStepCount)
                return;

            _steps.Add(new Step(lineNumber));
        }

        public static void ReportRefValue<T>(ref T value, string name, int lineNumber) {
            ReportValue(value, name, lineNumber);
        }

        public static void ReportValue<T>(T value, string name, int lineNumber) {
            var notes = PrepareToReportValue(name, lineNumber);
            if (notes == null)
                return;
            ValuePresenter.AppendTo(notes, value, ReportLimits.ValueValue);
        }

        public static void ReportRefSpanValue<T>(ref Span<T> value, string name, int lineNumber) {
            ReportSpanValue(value, name, lineNumber);
        }

        public static void ReportSpanValue<T>(Span<T> value, string name, int lineNumber) {
            var notes = PrepareToReportValue(name, lineNumber);
            if (notes == null)
                return;
            ValuePresenter.AppendTo(notes, (ReadOnlySpan<T>)value, ReportLimits.ValueValue);
        }

        public static void ReportRefReadOnlySpanValue<T>(ref ReadOnlySpan<T> value, string name, int lineNumber) {
            ReportReadOnlySpanValue(value, name, lineNumber);
        }

        public static void ReportReadOnlySpanValue<T>(ReadOnlySpan<T> value, string name, int lineNumber) {
            var notes = PrepareToReportValue(name, lineNumber);
            if (notes == null)
                return;
            ValuePresenter.AppendTo(notes, value, ReportLimits.ValueValue);
        }

        private static StringBuilder PrepareToReportValue(string name, int lineNumber) {
            if (!TryFindLastStepAtLineNumber(lineNumber, out var step, out var stepIndex)) {
                if (_steps.Count >= ReportLimits.MaxStepCount)
                    return null;
                step = new Step(lineNumber) { LineSkipped = true };
                _steps.Add(step);
                stepIndex = _steps.Count - 1;
            }

            if (!_stepNotesCountPerLine.TryGetValue(step.LineNumber, out var countPerLine))
                countPerLine = 0;

            if (step.Notes == null) {
                countPerLine += 1;
                _stepNotesCountPerLine[step.LineNumber] = countPerLine;

                if (countPerLine == ReportLimits.MaxStepNotesPerLine + 1) {
                    step.Notes = new StringBuilder("…");
                    _steps[stepIndex] = step;
                    return null;
                }
            }

            if (countPerLine >= ReportLimits.MaxStepNotesPerLine + 1)
                return null;

            step.ValueCount += 1;
            _steps[stepIndex] = step;
            if (step.ValueCount > ReportLimits.MaxValuesPerStep + 1)
                return null;

            var notes = step.Notes;
            if (notes == null) {
                notes = new StringBuilder();
                step.Notes = notes;
                _steps[stepIndex] = step;
            }

            if (notes.Length > 0)
                notes.Append(", ");

            if (step.ValueCount == ReportLimits.MaxValuesPerStep + 1) {
                notes.Append("…");
                return null;
            }

            if (name != null) {
                ValuePresenter.AppendStringTo(notes, name, ReportLimits.ValueName);
                notes.Append(": ");
            }

            return notes;
        }

        private static bool TryFindLastStepAtLineNumber(int lineNumber, out Step step, out int stepIndex) {
            for (var i = _steps.Count - 1; i >= 0; i--) {
                step = _steps[i];
                if (step.LineNumber == lineNumber || lineNumber == UnknownLineNumber) {
                    stepIndex = i;
                    return true;
                }
            }
            step = default(Step);
            stepIndex = -1;
            return false;
        }

        public static void ReportException(object exception) {
            if (_steps.Count == 0)
                return;
            var step = _steps[_steps.Count - 1];
            step.Exception = exception;
            _steps[_steps.Count - 1] = step;
        }

        internal static int? GetLastReportedLineNumber() {
            if (_steps.Count == 0)
                return null;

            return _steps[_steps.Count - 1].LineNumber;
        }

        [Serializable]
        public struct Step {
            public Step(int lineNumber) {
                LineNumber = lineNumber;
                Notes = null;
                Exception = null;
                ValueCount = 0;
                LineSkipped = false;
            }

            public int LineNumber { get; }
            public object Exception { get; internal set; }
            public StringBuilder Notes { get; internal set; }
            public bool LineSkipped { get; internal set; }

            internal int ValueCount { get; set; }
        }
    }
}
