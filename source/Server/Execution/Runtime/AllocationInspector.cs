using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.Runtime;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common;

namespace SharpLab.Server.Execution.Runtime {
    [Obsolete("Only used as a reference for future allocation support in Container.", true)]
    public class AllocationInspector : IAllocationInspector {
        private const string NullTypeName = "<unknown type>";
        private static readonly SimpleInspection NullType = new(NullTypeName);

        private readonly IValuePresenter _valuePresenter;
        private readonly Pool<ClrRuntime> _runtimePool;

        public AllocationInspector(Pool<ClrRuntime> runtimePool, IValuePresenter valuePresenter) {
            _runtimePool = runtimePool;
            _valuePresenter = valuePresenter;
        }

        public unsafe IInspection InspectAllocations(Action action) {
            if (!ProfilerState.Active)
                throw new NotSupportedException("Inspect.Allocations is only supported in .NET Core Profiler mode.");

            try {
                ProfilerNativeMethods.StartMonitoringCurrentThreadAllocations();
            }
            finally {
                ProfilerNativeMethods.StopMonitoringCurrentThreadAllocations(out var _, out var _, out var _, out var _);
            }

            int allocationCount;
            void* allocations;
            int totalAllocationCount;
            int totalAllocationBytes;
            ProfilerNativeMethods.AllocationMonitoringResult result;
            try {
                ProfilerNativeMethods.StartMonitoringCurrentThreadAllocations();
                action();
            }
            finally {
                result = ProfilerNativeMethods.StopMonitoringCurrentThreadAllocations(
                    out allocationCount,
                    out allocations,
                    out totalAllocationCount,
                    out totalAllocationBytes
                );
            }

            if (result == ProfilerNativeMethods.AllocationMonitoringResult.GC)
                Output.WriteWarning("Garbage collection has happened while retrieving allocations. Please try again.");

            if (allocationCount == 0)
                return new SimpleInspection("Allocations", "None");

            using var runtimeLease = _runtimePool.GetOrCreate();
            var runtime = runtimeLease.Object;
            runtime.FlushCachedData();

            var inspections = new List<IInspection>(allocationCount);
            foreach (var allocationPointer in new Span<IntPtr>(allocations, allocationCount)) {
                // Note that profiler returns allocations pointers pointing to the start of the object
                // and not after initial header, as CLR does.
                var objectPointer = allocationPointer + IntPtr.Size /* object header size */;
                var @object = runtime.Heap.GetObject(unchecked((ulong)objectPointer.ToInt64()));

                inspections.Add(InspectClrObject(@object));
            }

            var title = "Allocations: " + totalAllocationCount + " (" + totalAllocationBytes + " bytes)";
            return new InspectionGroup(title, inspections, limitReached: totalAllocationCount > allocationCount);
        }

        private SimpleInspection InspectClrObject(ClrObject value) => InspectAddress(value.Type, value.Address);

        private unsafe SimpleInspection InspectAddress(ClrType? type, ulong address) {
            return type?.ElementType switch
            {
                ClrElementType.String => InspectString(address),
                ClrElementType.Int8 => InspectPrimitive<sbyte>(address),
                ClrElementType.Int16 => InspectPrimitive<short>(address),
                ClrElementType.Int32 => InspectPrimitive<int>(address),
                ClrElementType.Int64 => InspectPrimitive<long>(address),
                ClrElementType.UInt8 => InspectPrimitive<byte>(address),
                ClrElementType.UInt16 => InspectPrimitive<ushort>(address),
                ClrElementType.UInt32 => InspectPrimitive<uint>(address),
                ClrElementType.UInt64 => InspectPrimitive<ulong>(address),
                ClrElementType.Float => InspectPrimitive<float>(address),
                ClrElementType.Double => InspectPrimitive<double>(address),
                ClrElementType.Boolean => InspectPrimitive<bool>(address),
                ClrElementType.Char => InspectPrimitive<char>(address),
                ClrElementType.NativeInt => InspectPrimitive<IntPtr>(address),
                ClrElementType.NativeUInt => InspectPrimitive<UIntPtr>(address),
                ClrElementType.SZArray => InspectArray(type, address),
                _ => InspectOther(type)
            };
        }

        private unsafe SimpleInspection InspectString(ulong address) {
            var headerSize = (uint)IntPtr.Size;
            var stringLength = *(int*)(address + headerSize);
            var stringStart = address + headerSize + sizeof(int);
            var bytes = new ReadOnlySpan<byte>((void*)stringStart, stringLength * 2);
            return new SimpleInspection("String", Encoding.Unicode.GetString(bytes));
        }

        private unsafe SimpleInspection InspectPrimitive<T>(ulong address)
            where T : unmanaged
        {
            var headerSize = (uint)IntPtr.Size;
            var valueStartAddress = address + headerSize;
            return new SimpleInspection(FormatBoxedName(typeof(T).Name), (*(T*)valueStartAddress).ToString()!);
        }

        private SimpleInspection InspectArray(ClrType type, ulong address) {
            var componentType = type.ComponentType!;
            if (componentType.IsObjectReference || !componentType.IsPrimitive)
                return InspectOther(type);

            var array = type.Heap.GetObject(address).AsArray();

            SimpleInspection InspectArrayOf<T>()
                where T: unmanaged
            {
                var builder = new StringBuilder();
                var values = array.ReadValues<T>(0, array.Length);
                if (values != null) {
                    _valuePresenter.AppendEnumerableTo(
                        builder, values,
                        depth: 1,
                        new ValuePresenterLimits(maxEnumerableItemCount: 10, maxValueLength: 10)
                    );
                }
                else {
                    builder.Append("<unknown array values>");
                }

                var title = (componentType.Name?.Replace("System.", "") ?? NullTypeName) + "[" + array.Length + "]";
                return new SimpleInspection(title, builder);
            }

            return componentType.ElementType switch {
                ClrElementType.Int8 => InspectArrayOf<sbyte>(),
                ClrElementType.Int16 => InspectArrayOf<short>(),
                ClrElementType.Int32 => InspectArrayOf<int>(),
                ClrElementType.Int64 => InspectArrayOf<long>(),
                ClrElementType.UInt8 => InspectArrayOf<byte>(),
                ClrElementType.UInt16 => InspectArrayOf<ushort>(),
                ClrElementType.UInt32 => InspectArrayOf<uint>(),
                ClrElementType.UInt64 => InspectArrayOf<ulong>(),
                ClrElementType.Float => InspectArrayOf<float>(),
                ClrElementType.Double => InspectArrayOf<double>(),
                ClrElementType.Boolean => InspectArrayOf<bool>(),
                ClrElementType.Char => InspectArrayOf<char>(),
                ClrElementType.NativeInt => InspectArrayOf<IntPtr>(),
                ClrElementType.NativeUInt => InspectArrayOf<UIntPtr>(),
                _ => InspectOther(type)
            };
        }

        private SimpleInspection InspectOther(ClrType? type) {
            if (type?.Name == null)
                return NullType;

            var title = type.Name;
            if (!type.IsObjectReference)
                title = FormatBoxedName(title);

            return new SimpleInspection(title);

            /*
            var fields = type.Fields;
            var fieldInspections = new IInspection[fields.Count];
            for (var i = 0; i < fields.Count; i++) {
                var field = fields[i];
                var valueAddress = field.GetAddress(address);
                if (!field.Type.IsObjectReference) {
                    fieldInspections[i] = Inspect(field.Type, valueAddress, field.Name);
                }
                else {
                    fieldInspections[i] = new SimpleInspection(field.Name, string.Format("<at 0x{0:X}>", valueAddress));
                }
            }

            return new InspectionGroup(name ?? type.Name, fieldInspections);*/
        }

        private static string FormatBoxedName(string name) => name + " (boxed)";
    }
}