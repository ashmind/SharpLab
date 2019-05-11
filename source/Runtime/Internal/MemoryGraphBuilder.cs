using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpLab.Runtime.Internal {
    internal class MemoryGraphBuilder {
        private readonly IList<MemoryGraphNode> _stack = new List<MemoryGraphNode>();
        private readonly IList<MemoryGraphNode> _heap = new List<MemoryGraphNode>();
        private readonly IList<MemoryGraphReference> _references = new List<MemoryGraphReference>();

        private readonly IReadOnlyList<string> _argumentNames;
        private int _nextArgumentIndex;

        public MemoryGraphBuilder(IReadOnlyList<string> argumentNames) {
            _argumentNames = argumentNames;
            _nextArgumentIndex = 0;
        }

        public unsafe MemoryGraphBuilder Add<T>(in T value) {
            var stackAddress = (ulong)Unsafe.AsPointer(ref Unsafe.AsRef(in value));
            var stackSize = Unsafe.SizeOf<T>();

            var name = _argumentNames.ElementAtOrDefault(_nextArgumentIndex);
            _nextArgumentIndex += 1;

            var isStack = typeof(T).IsValueType || value == null;

            var stackNode = isStack
                ? MemoryGraphNode.OnStack(name, ValuePresenter.ToStringBuilder(value), stackAddress, stackSize)
                : MemoryGraphNode.OnStack(name, GetTitleForReference(typeof(T)), stackAddress, stackSize);
            _stack.Add(stackNode);

            if (isStack) {
                AddNestedNodes(stackNode, value);
            }
            else {
                AddHeapObject(stackNode, value);
            }
            return this;
        }

        private void AddHeapObject(MemoryGraphNode referencingNode, object @object) {
            var objectType = @object.GetType();

            var heapNode = FindExistingNode(@object);
            if (heapNode == null) {
                heapNode = MemoryGraphNode.OnHeap(objectType.Name, ValuePresenter.ToStringBuilder(@object), @object);
                _heap.Add(heapNode);

                AddNestedNodes(heapNode, @object);
            }

            _references.Add(new MemoryGraphReference(referencingNode, heapNode));
        }

        private void AddNestedNodes(MemoryGraphNode parentNode, object @object) {
            var objectType = @object.GetType();
            if (@object is string || objectType.IsPrimitive)
                return;

            if (objectType.IsArray) {
                var index = 0;
                var itemType = objectType.GetElementType();
                foreach (var item in (IEnumerable)@object) {
                    if (!parentNode.ValidateNestedLimit())
                        break;

                    AddNestedNode(parentNode, index.ToString(), itemType, item);
                    index += 1;
                }
                return;
            }

            var fields = objectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields) {
                if (!parentNode.ValidateNestedLimit())
                    break;

                AddNestedNode(parentNode, field.Name, field.FieldType, field.GetValue(@object));
            }
        }

        private void AddNestedNode(MemoryGraphNode parentNode, string title, Type staticType, object value)
        {
            var isReference = !staticType.IsValueType && value != null;
            var nestedNode = isReference
                ? MemoryGraphNode.Nested(title, GetTitleForReference(staticType))
                : MemoryGraphNode.Nested(title, ValuePresenter.ToStringBuilder(value));
            parentNode.AddNestedNode(nestedNode);
            if (isReference)
                AddHeapObject(nestedNode, value);
        }

        private MemoryGraphNode? FindExistingNode(object heapKey) {
            // not using LINQ to avoid allocations
            // should be faster than creating a dictionary, given we only have a few nodes
            foreach (var node in _heap) {
                if (node.HeapKey == heapKey)
                    return node;
            }
            return null;
        }

        private string GetTitleForReference(Type staticType) {
            return staticType.Name + " ref";
        }

        public MemoryGraphInspection ToInspection() {
            var minStackAddress = ulong.MaxValue;
            foreach (var node in _stack) {
                minStackAddress = Math.Min(node.StackAddress!.Value, minStackAddress);
            }
            foreach (var node in _stack) {
                node.StackOffset = (int)(node.StackAddress - minStackAddress)!;
            }

            return new MemoryGraphInspection(
                (IReadOnlyList<MemoryGraphNode>)_stack,
                (IReadOnlyList<MemoryGraphNode>)_heap,
                (IReadOnlyList<MemoryGraphReference>)_references
            );
        }
    }
}
