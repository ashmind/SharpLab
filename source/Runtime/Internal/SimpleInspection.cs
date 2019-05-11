using System;
using System.Text;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class SimpleInspection : IInspection {
        public SimpleInspection(string title) {
            Title = title;
            HasValue = false;
        }

        public SimpleInspection(string title, string value) {
            Title = title;
            Value = value;
            HasValue = true;
        }

        public SimpleInspection(string title, StringBuilder value) {
            Title = title;
            Value = value;
            HasValue = true;
        }

        public string Title { get; }
        public object? Value { get; }
        public bool HasValue { get; }
    }
}