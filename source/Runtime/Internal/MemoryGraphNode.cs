using System;
using System.Collections.Generic;
using System.Text;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class MemoryGraphNode {
        private List<MemoryGraphNode>? _nestedNodes = null;

        private MemoryGraphNode(
            int id,
            string? title,
            object value,
            object? heapKey = null,
            ulong? stackAddress = null,
            int? stackSize = null
        ) {
            Id = id;
            Title = title;
            Value = value;
            HeapKey = heapKey;
            StackAddress = stackAddress;
            StackSize = stackSize;
        }

        public static MemoryGraphNode OnStack(int id, string? title, StringBuilder value, ulong stackAddress, int stackSize) {
            return new MemoryGraphNode(id, title, value, stackAddress: stackAddress, stackSize: stackSize);
        }

        public static MemoryGraphNode OnStack(int id, string? title, string value, ulong stackAddress, int stackSize) {
            return new MemoryGraphNode(id, title, value, stackAddress: stackAddress, stackSize: stackSize);
        }

        public static MemoryGraphNode OnHeap(int id, string title, StringBuilder value, object heapKey) {
            return new MemoryGraphNode(id, title, value, heapKey: heapKey);
        }

        public static MemoryGraphNode Nested(int id, string title, StringBuilder value) {
            return new MemoryGraphNode(id, title, value);
        }

        public static MemoryGraphNode Nested(int id, string title, string value) {
            return new MemoryGraphNode(id, title, value);
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
        public string? Title { get; }
        public object Value { get; }
        public int? StackOffset { get; set; }
        public int? StackSize { get; }
        public IReadOnlyList<MemoryGraphNode> NestedNodes =>
            _nestedNodes ?? (IReadOnlyList<MemoryGraphNode>)Array.Empty<MemoryGraphNode>();
        public bool NestedNodesLimitReached { get; private set; }

        [field: NonSerialized]
        public object? HeapKey { get; }
        [field: NonSerialized]
        public ulong? StackAddress { get; }
    }
}
