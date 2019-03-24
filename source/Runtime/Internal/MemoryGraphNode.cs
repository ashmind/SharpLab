using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class MemoryGraphNode {
        private List<MemoryGraphNode> _nestedNodes = null;

        private static int NextId = 1;

        private MemoryGraphNode(
            string title,
            object value,
            object heapKey = null,
            ulong? stackAddress = null,
            int? stackSize = null
        ) {
            Id = NextId;
            Title = title;
            Value = value;
            HeapKey = heapKey;
            StackAddress = stackAddress;
            StackSize = stackSize;

            NextId += 1;
        }

        public static MemoryGraphNode OnStack(string title, StringBuilder value, ulong stackAddress, int stackSize) {
            return new MemoryGraphNode(title, value, stackAddress: stackAddress, stackSize: stackSize);
        }

        public static MemoryGraphNode OnStack(string title, string value, ulong stackAddress, int stackSize) {
            return new MemoryGraphNode(title, value, stackAddress: stackAddress, stackSize: stackSize);
        }

        public static MemoryGraphNode OnHeap(string title, StringBuilder value, object heapKey) {
            return new MemoryGraphNode(title, value, heapKey: heapKey);
        }

        public static MemoryGraphNode Nested(string title, StringBuilder value) {
            return new MemoryGraphNode(title, value);
        }

        public static MemoryGraphNode Nested(string title, string value) {
            return new MemoryGraphNode(title, value);
        }

        public bool ValidateNestedLimit() {
            if (_nestedNodes?.Count == 3) {
                NestedNodesLimitReached = true;
                return false;
            }

            return true;
        }

        public void AddNestedNode(MemoryGraphNode nested) {
            if (_nestedNodes == null)
                _nestedNodes = new List<MemoryGraphNode>();

            if (!ValidateNestedLimit())
                throw new Exception("Attempted to add nested node over the limit.");

            _nestedNodes.Add(nested);
        }

        public int Id { get; }
        public string Title { get; }
        public object Value { get; }
        public int? StackOffset { get; set; }
        public int? StackSize { get; }
        public IReadOnlyList<MemoryGraphNode> NestedNodes =>
            _nestedNodes ?? (IReadOnlyList<MemoryGraphNode>)Array.Empty<MemoryGraphNode>();
        public bool NestedNodesLimitReached { get; private set; }

        [field: NonSerialized]
        public object HeapKey { get; }
        [field: NonSerialized]
        public ulong? StackAddress { get; }
    }
}
