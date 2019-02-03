using System;
using System.Collections.Generic;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class MemoryGraphInspection : IInspection {
        public MemoryGraphInspection(
            IReadOnlyList<MemoryGraphNode> stack,
            IReadOnlyList<MemoryGraphNode> heap,
            IReadOnlyList<MemoryGraphReference> references
        ) {
            Stack = stack;
            Heap = heap;
            References = references;
        }

        public IReadOnlyList<MemoryGraphNode> Stack { get; }
        public IReadOnlyList<MemoryGraphNode> Heap { get; }
        public IReadOnlyList<MemoryGraphReference> References { get; }
    }
}
