using System;
using System.Collections.Generic;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public readonly struct MemoryInspectionLabel {
        public MemoryInspectionLabel(string name, int offset, int length, IReadOnlyList<MemoryInspectionLabel> nested = null) {
            Name = name;
            Offset = offset;
            Length = length;
            Nested = nested ?? Array.Empty<MemoryInspectionLabel>();
        }

        public string Name { get; }
        public int Offset { get; }
        public int Length { get; }
        public IReadOnlyList<MemoryInspectionLabel> Nested { get; }
    }
}
