using System;
using System.Text;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class SimpleInspection : IInspection {
        public SimpleInspection(string title, string value) {
            Title = title;
            Value = value;
        }

        public SimpleInspection(string title, StringBuilder value) {
            Title = title;
            Value = value;
        }

        public string Title { get; }
        public object Value { get; }
    }
}