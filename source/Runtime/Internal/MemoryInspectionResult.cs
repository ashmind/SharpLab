using System;
using System.Collections.Generic;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class MemoryInspectionResult {
        public MemoryInspectionResult(string title, IReadOnlyList<Label> labels, byte[] data) {
            Title = title;
            Labels = labels;
            Data = data;
        }

        public string Title { get; }
        public IReadOnlyList<Label> Labels { get; }
        public byte[] Data { get; }

        [Serializable]
        public readonly struct Label {
            public Label(string name, int offset, int length) {
                Name = name;
                Offset = offset;
                Length = length;
            }

            public string Name { get; }
            public int Offset { get; }
            public int Length { get; }
        }
    }
}
