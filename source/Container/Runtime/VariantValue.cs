using System.Runtime.InteropServices;

namespace SharpLab.Container.Runtime {
    internal readonly struct VariantValue {
        private readonly VariantKind _kind;
        private readonly object? _objectValue;
        private readonly Union _unionValue;

        private VariantValue(object? objectValue) {
            _kind = VariantKind.Object;
            _objectValue = objectValue;
            _unionValue = default;
        }

        private VariantValue(Union unionValue, VariantKind kind) {
            _kind = kind;
            _unionValue = unionValue;
            _objectValue = null;
        }

        public static VariantValue From<T>(T value) => value switch {
            (int i) => new(new(i), VariantKind.Int32),
            (long l) => new(new(l), VariantKind.Int64),
            _ => new(value)
        };

        public int AsInt32Unchecked() => _unionValue.AsInt32Unchecked();
        public long AsInt64Unchecked() => _unionValue.AsInt64Unchecked();
        public object? AsObjectUnchecked() => _objectValue;

        public VariantKind Kind => _kind;

        [StructLayout(LayoutKind.Explicit)]
        private readonly struct Union {
            [FieldOffset(0)] private readonly int _int32Value;
            [FieldOffset(1)] private readonly long _int64Value;

            public Union(int value) : this() {
                _int32Value = value;
            }

            public Union(long value) : this() {
                _int64Value = value;
            }

            public int AsInt32Unchecked() => _int32Value;
            public long AsInt64Unchecked() => _int64Value;
        }
    }
}
