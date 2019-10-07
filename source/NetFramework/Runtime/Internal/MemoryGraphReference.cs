using System;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class MemoryGraphReference {
        public MemoryGraphReference(MemoryGraphNode from, MemoryGraphNode to) {
            From = from;
            To = to;
        }

        public MemoryGraphNode From { get; }
        public MemoryGraphNode To { get; }
    }
}
