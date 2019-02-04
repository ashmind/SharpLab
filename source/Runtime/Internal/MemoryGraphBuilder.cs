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
            var stackSize = typeof(T).IsValueType ? Marshal.SizeOf(value) : IntPtr.Size;

            var name = _argumentNames.ElementAtOrDefault(_nextArgumentIndex);
            _nextArgumentIndex += 1;

            var isStack = typeof(T).IsValueType || value == null;

            var stackNode = isStack
                ? MemoryGraphNode.OnStack(name, ValuePresenter.ToStringBuilder(value), stackAddress, stackSize)
                : MemoryGraphNode.OnStack(name, GetTitleForReference(typeof(T)), stackAddress, stackSize);
            _stack.Add(stackNode);

            if (isStack) {
                AddNestedReferences(stackNode, value);
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

                AddNestedReferences(heapNode, @object);
            }

            _references.Add(new MemoryGraphReference(referencingNode, heapNode));
        }

        private void AddNestedReferences(MemoryGraphNode parentNode, object @object) {
            var objectType = @object.GetType();
            if (objectType.IsArray) {
                var index = 0;
                var staticItemType = objectType.GetElementType();
                var arrayIsValue = staticItemType.IsValueType;
                foreach (var item in (IEnumerable)@object) {
                    if (!parentNode.ValidateNestedLimit())
                        break;

                    var itemType = item.GetType();
                    var itemIsValue = arrayIsValue || item == null;
                    var itemNode = itemIsValue
                        ? MemoryGraphNode.Nested(index.ToString(), ValuePresenter.ToStringBuilder(item))
                        : MemoryGraphNode.Nested(index.ToString(), GetTitleForReference(staticItemType));
                    parentNode.AddNestedNode(itemNode);
                    if (!itemIsValue)
                        AddHeapObject(itemNode, item);
                    index += 1;
                }
                return;
            }

            var fields = objectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields) {
                if (field.FieldType.IsValueType)
                    continue;

                var value = field.GetValue(@object);
                if (value == null)
                    continue;

                if (!parentNode.ValidateNestedLimit())
                    break;

                var fieldNode = MemoryGraphNode.Nested(field.Name, GetTitleForReference(field.FieldType));
                parentNode.AddNestedNode(fieldNode);
                AddHeapObject(fieldNode, value);
            }
        }

        private MemoryGraphNode FindExistingNode(object heapKey) {
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
                minStackAddress = Math.Min(node.StackAddress.Value, minStackAddress);
            }
            foreach (var node in _stack) {
                node.StackOffset = (int)(node.StackAddress - minStackAddress);
            }

            return new MemoryGraphInspection(
                (IReadOnlyList<MemoryGraphNode>)_stack,
                (IReadOnlyList<MemoryGraphNode>)_heap,
                (IReadOnlyList<MemoryGraphReference>)_references
            );
        }
    }
}
