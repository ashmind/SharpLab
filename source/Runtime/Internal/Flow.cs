using System;
using System.Collections.Generic;
using System.Text;

namespace SharpLab.Runtime.Internal {
    public static class Flow {
        public const int UnknownLineNumber = -1;

        private static class ReportLimits {
            public const int MaxStepCount = 50;
            public const int MaxValueNameLength = 10;
            public const int MaxValueValueLength = 10;
            public const int MaxEnumerableItems = 3;
            public const int MaxStepNotesPerLine = 3;
            public const int MaxValuesPerStep = 3;
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
            if (!TryFindLastStepAtLineNumber(lineNumber, out var step, out var stepIndex)) {
                if (_steps.Count >= ReportLimits.MaxStepCount)
                    return;
                step = new Step(lineNumber) { LineSkipped = true };
                _steps.Add(step);
                stepIndex = _steps.Count - 1;
            }

            if (!_stepNotesCountPerLine.TryGetValue(step.LineNumber, out int countPerLine))
                countPerLine = 0;

            if (step.Notes == null) {
                countPerLine += 1;
                _stepNotesCountPerLine[step.LineNumber] = countPerLine;

                if (countPerLine == ReportLimits.MaxStepNotesPerLine + 1) {
                    step.Notes = new StringBuilder("…");
                    _steps[stepIndex] = step;
                    return;
                }
            }

            if (countPerLine >= ReportLimits.MaxStepNotesPerLine + 1)
                return;

            step.ValueCount += 1;
            if (step.ValueCount > ReportLimits.MaxValuesPerStep + 1) {
                _steps[stepIndex] = step;
                return;
            }

            var notes = step.Notes;
            if (notes == null) {
                notes = new StringBuilder();
                step.Notes = notes;
            }

            if (notes.Length > 0)
                notes.Append(", ");

            if (step.ValueCount == ReportLimits.MaxValuesPerStep + 1) {
                notes.Append("…");
                _steps[stepIndex] = step;
                return;
            }
            
            if (name != null) {
                ObjectAppender.AppendString(notes, name, ReportLimits.MaxValueNameLength);
                notes.Append(": ");
            }
            ObjectAppender.Append(notes, value, ReportLimits.MaxEnumerableItems, ReportLimits.MaxValueValueLength);
            // Have to reassign in case we set Notes
            _steps[stepIndex] = step;
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
