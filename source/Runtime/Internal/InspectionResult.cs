using System;
using System.Text;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class InspectionResult {
        public InspectionResult(string title, StringBuilder value) {
            Title = title;
            Value = value;
        }

        public string Title { get; }
        public StringBuilder Value { get; }
    }
}