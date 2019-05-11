#if NETCORE

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.Runtime;

namespace SharpLab.Runtime.Internal {
    internal static class AllocationInspector {
        public static IInspection Inspect(ClrObject value) => Inspect(value.Type, value.Address);

        private unsafe static IInspection Inspect(ClrType type, ulong address) {
            return type.ElementType switch
            {
                ClrElementType.String => String(address),
                ClrElementType.Int8 => Primitive<sbyte>(address),
                ClrElementType.Int16 => Primitive<short>(address),
                ClrElementType.Int32 => Primitive<int>(address),
                ClrElementType.Int64 => Primitive<long>(address),
                ClrElementType.UInt8 => Primitive<byte>(address),
                ClrElementType.UInt16 => Primitive<ushort>(address),
                ClrElementType.UInt32 => Primitive<uint>(address),
                ClrElementType.UInt64 => Primitive<ulong>(address),
                ClrElementType.Float => Primitive<float>(address),
                ClrElementType.Double => Primitive<double>(address),
                ClrElementType.Boolean => Primitive<bool>(address),
                ClrElementType.Char => Primitive<char>(address),
                ClrElementType.NativeInt => Primitive<IntPtr>(address),
                ClrElementType.NativeUInt => Primitive<UIntPtr>(address),
                ClrElementType.SZArray => Array(type, address),
                _ => Other(type)
            };
        }

        private unsafe static IInspection String(ulong address) {
            var headerSize = (uint)IntPtr.Size;
            var stringLength = *(int*)(address + headerSize);
            var stringStart = address + headerSize + sizeof(int);
            var bytes = new ReadOnlySpan<byte>((void*)stringStart, stringLength * 2);
            return new SimpleInspection("String", Encoding.Unicode.GetString(bytes));
        }

        private static unsafe IInspection Primitive<T>(ulong address)
            where T : unmanaged
        {
            var headerSize = (uint)IntPtr.Size;
            var valueStartAddress = address + headerSize;
            return new SimpleInspection(FormatBoxedName(typeof(T).Name), (*(T*)valueStartAddress).ToString());
        }

        private static unsafe IInspection Array(ClrType type, ulong address) {
            var componentType = type.ComponentType;
            if (componentType.IsObjectReference || !componentType.IsPrimitive)
                return Other(type);

            var length = type.GetArrayLength(address);
            var builder = new StringBuilder();
            ValuePresenter.AppendEnumerableTo(
                builder, ArrayAsEnumerable(type, address, length),
                depth: 1,
                new ValuePresenterLimits(maxDepth: 2, maxEnumerableItemCount: 10, maxValueLength: 10)
            );

            var title = componentType.Name.Replace("System.", "") + "[" + length + "]";
            return new SimpleInspection(title, builder);
        }

        private static IEnumerable<object> ArrayAsEnumerable(ClrType arrayType, ulong address, int length) {
            for (var i = 0; i < length; i++) {
                yield return arrayType.GetArrayElementValue(address, i);
            }
        }

        private static IInspection Other(ClrType type) {
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

#endif