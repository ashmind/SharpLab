using System;
using System.Collections.Generic;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class MemoryInspection : IInspection {
        public MemoryInspection(string title, IReadOnlyList<MemoryInspectionLabel> labels, byte[] data) {
            Title = title;
            Labels = labels;
            Data = data;
        }

        public string Title { get; }
        public IReadOnlyList<MemoryInspectionLabel> Labels { get; }
        public byte[] Data { get; }
    }
}
