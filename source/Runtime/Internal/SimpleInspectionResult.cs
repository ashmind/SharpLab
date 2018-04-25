using System;
using System.Text;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class SimpleInspectionResult {
        public SimpleInspectionResult(string title, string value) {
            Title = title;
            Value = value;
        }

        public SimpleInspectionResult(string title, StringBuilder value) {
            Title = title;
            Value = value;
        }

        public string Title { get; }
        public object Value { get; }
    }
}