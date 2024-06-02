using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.Runtime;
using SharpLab.Runtime.Internal;

namespace SharpLab.Container.Runtime;

internal class MemoryBytesInspector : IMemoryBytesInspector {
    private readonly Pool<ClrRuntime> _runtimePool;

    public MemoryBytesInspector(Pool<ClrRuntime> runtimePool) {
        _runtimePool = runtimePool;
    }

    public MemoryInspection InspectHeap(object @object) {
        if (@object == null)
            throw new ArgumentNullException(nameof(@object), $"Inspect.Heap can't inspect null, as it does not point to a valid location on the heap.");

        using var runtimeLease = _runtimePool.GetOrCreate();
        var runtime = runtimeLease.Object;
        runtime.FlushCachedData();

        var address = (ulong)GetHeapPointer(@object);
        var objectType = runtime.Heap.GetObjectType(address);
        if (objectType == null)
            throw new ClrInformationNotFoundException($"Failed to find object type for address 0x{address:X}.");

        var objectSize = runtime.Heap.GetObject(address, objectType).Size;

        // Move by one pointer size back -- Object Header,
        // see https://blogs.msdn.microsoft.com/seteplia/2017/05/26/managed-object-internals-part-1-layout/
        //
        // Not sure if there is a better way to get this through ClrMD yet.
        // https://github.com/Microsoft/clrmd/issues/99
        var objectStart = address - (uint)IntPtr.Size;
        var data = ReadMemory(runtime, objectStart, objectSize);

        var labels = CreateLabelsFromType(objectType, address, objectStart, first: (index: 2, offset: 2 * IntPtr.Size));
        labels[0] = new MemoryInspectionLabel("header", 0, IntPtr.Size);
        labels[1] = new MemoryInspectionLabel("type handle", IntPtr.Size, IntPtr.Size);

        return new MemoryInspection($"{objectType.Name} at 0x{address:X}", labels, data);
    }

    public unsafe MemoryInspection InspectStack<T>(in T value) {
        using var runtimeLease = _runtimePool.GetOrCreate();
        var runtime = runtimeLease.Object;

        var type = typeof(T);

        var address = (ulong)Unsafe.AsPointer(ref Unsafe.AsRef(in value));
        var size = type.IsValueType ? (ulong)Unsafe.SizeOf<T>() : (uint)IntPtr.Size;
        var data = ReadMemory(runtime, address, size);

        MemoryInspectionLabel[] labels;
        if (type.IsValueType && !type.IsPrimitive) {
            runtime.FlushCachedData();
            var methodTableAddress = (ulong)type.TypeHandle.Value;
            var runtimeType = runtime.GetTypeByMethodTable(methodTableAddress)
                           ?? throw new ClrInformationNotFoundException($"Could not find type by method table at 0x{methodTableAddress:X}");
            labels = CreateLabelsFromType(runtimeType, address, address + (uint)IntPtr.Size);
        }
        else {
            labels = Array.Empty<MemoryInspectionLabel>();
        }

        var title = type.IsValueType
            ? $"{type.FullName}"
            : $"Pointer to {type.FullName}";

        return new MemoryInspection(title, labels, data);
    }

    private static byte[] ReadMemory(ClrRuntime runtime, ulong address, ulong size) {
        var data = new byte[size];
        runtime.DataTarget!.DataReader.Read(address, data);
        return data;
    }

    private MemoryInspectionLabel[] CreateLabelsFromType(
        ClrType objectType,
        ulong objectAddress,
        ulong offsetBase,
        (int index, int offset) first = default
    ) {
        MemoryInspectionLabel[] labels;
        if (objectType.IsArray) {
            var length = objectType.Heap.GetObject(objectAddress).AsArray().Length;
            labels = new MemoryInspectionLabel[first.index + 1 + length];
            labels[first.index] = new MemoryInspectionLabel("length", first.offset, IntPtr.Size);
            for (var i = 0; i < length; i++) {
                var elementAddress = objectType.GetArrayElementAddress(objectAddress, i);
                var offset = (int)(elementAddress - offsetBase);
                labels[first.index + 1 + i] = new MemoryInspectionLabel(
                    i.ToString(),
                    offset,
                    objectType.ComponentSize,
                    GetNestedLabels(objectType.ComponentType!, elementAddress, offsetBase)
                );
            }
            return labels;
        }

        var fields = objectType.Fields;
        var fieldCount = fields.Length;
        labels = new MemoryInspectionLabel[first.index + fieldCount];
        for (var i = 0; i < fieldCount; i++) {
            var field = fields[i];
            if (field.Type == null)
                throw new ClrInformationNotFoundException($"Could not get type for field {field.Name}.");

            var fieldAddress = field.GetAddress(objectAddress);
            var offset = (int)(fieldAddress - offsetBase);
            labels[first.index + i] = new MemoryInspectionLabel(
                field.Name ?? "",
                offset,
                field.Size,
                GetNestedLabels(field.Type, fieldAddress, offsetBase)
            );
        }
        return labels;
    }

    private IReadOnlyList<MemoryInspectionLabel> GetNestedLabels(ClrType type, ulong valueAddress, ulong offsetBase) {
        if (type.IsPrimitive || !type.IsValueType)
            return Array.Empty<MemoryInspectionLabel>();

        return CreateLabelsFromType(type, valueAddress, offsetBase + (uint)IntPtr.Size);
    }

    private static unsafe IntPtr GetHeapPointer(object @object) {
        var indirect = Unsafe.AsPointer(ref @object);
        return **(IntPtr**)(&indirect);
    }
}
