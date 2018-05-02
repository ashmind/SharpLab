using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Diagnostics.Runtime;
using SharpLab.Runtime.Internal;

public static class Inspect {
    private static ClrRuntime _runtime;

    public static void Heap(object @object) {
        EnsureRuntime();

        var address = (ulong)GetHeapPointer(@object);
        var objectType = _runtime.Heap.GetObjectType((ulong)GetHeapPointer(@object));
        if (objectType == null)
            throw new Exception($"Failed to find object type for address 0x{(ulong)GetHeapPointer(@object):X}.");

        var objectSize = objectType.GetSize(address);

        // Move by one pointer size back -- Object Header,
        // see https://blogs.msdn.microsoft.com/seteplia/2017/05/26/managed-object-internals-part-1-layout/
        //
        // Not sure if there is a better way to get this through ClrMD yet.
        // https://github.com/Microsoft/clrmd/issues/99
        var objectStart = address - (uint)IntPtr.Size;
        var data = ReadMemory(objectStart, objectSize);

        var labels = CreateLabelsFromType(objectType, address, objectStart, first: (index: 2, offset: 2 * IntPtr.Size));
        labels[0] = new MemoryInspectionResult.Label("header", 0, IntPtr.Size);
        labels[1] = new MemoryInspectionResult.Label("type handle", IntPtr.Size, IntPtr.Size);

        Output.Write(new MemoryInspectionResult($"{objectType.Name} at 0x{address:X}", labels, data));
    }

    private static byte[] ReadMemory(ulong address, ulong size) {
        EnsureRuntime();

        var data = new byte[size];
        _runtime.ReadMemory(address, data, (int)size, out var _);
        return data;
    }

    private static unsafe IntPtr GetHeapPointer(object @object) {
        var indirect = Unsafe.AsPointer(ref @object);
        return **(IntPtr**)(&indirect);
    }

    public static unsafe void Stack<T>(in T value) {
        var type = typeof(T);

        var address = (ulong)Unsafe.AsPointer(ref Unsafe.AsRef(in value));
        var size = type.IsValueType ? (ulong)Unsafe.SizeOf<T>() : (uint)IntPtr.Size;
        var data = ReadMemory(address, size);

        MemoryInspectionResult.Label[] labels;
        if (type.IsValueType && !type.IsPrimitive) {
            var runtimeType = _runtime.Heap.GetTypeByMethodTable((ulong)type.TypeHandle.Value);
            labels = CreateLabelsFromType(runtimeType, address, address + (uint)IntPtr.Size);
        }
        else {
            labels = new MemoryInspectionResult.Label[0];
        }

        var title = type.IsValueType
            ? $"{type.FullName}"
            : $"Pointer to {type.FullName}";
        Output.Write(new MemoryInspectionResult(title, labels, data));
    }

    private static MemoryInspectionResult.Label[] CreateLabelsFromType(ClrType objectType, ulong objectAddress, ulong offsetBase, (int index, int offset) first = default) {
        MemoryInspectionResult.Label[] labels;
        if (objectType.IsArray) {
            var length = objectType.GetArrayLength(objectAddress);
            labels = new MemoryInspectionResult.Label[first.index + 1 + length];
            labels[first.index] = new MemoryInspectionResult.Label("length", first.offset, IntPtr.Size);
            for (var i = 0; i < length; i++) {
                var offset = (int)(objectType.GetArrayElementAddress(objectAddress, i) - offsetBase);
                labels[first.index + 1 + i] = new MemoryInspectionResult.Label(
                    i.ToString(),
                    offset,
                    objectType.ElementSize
                );
            }
            return labels;
        }

        var fields = objectType.Fields;
        var fieldCount = fields.Count;
        labels = new MemoryInspectionResult.Label[first.index + fieldCount];
        for (var i = 0; i < fieldCount; i++) {
            var field = fields[i];
            var offset = (int)(field.GetAddress(objectAddress) - offsetBase);
            labels[first.index + i] = new MemoryInspectionResult.Label(
                field.Name,
                offset,
                field.Size
            );
        }
        return labels;
    }

    private static void EnsureRuntime() {
        if (_runtime != null)
            return;
        var dataTarget = DataTarget.AttachToProcess(InspectionSettings.CurrentProcessId, UInt32.MaxValue, AttachFlag.Passive);
        _runtime = dataTarget.ClrVersions.Single().CreateRuntime();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static new bool Equals(object a, object b) {
        throw new NotSupportedException();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static new bool ReferenceEquals(object objA, object objB) {
        throw new NotSupportedException();
    }
}