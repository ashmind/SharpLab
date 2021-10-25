using System;
using System.Buffers;

namespace SharpLab.Server.Common {
    public readonly struct MemoryLease<T> : IDisposable {
        private readonly ArrayPool<T> _arrayPool;
        private readonly T[] _array;
        private readonly int _length;

        public MemoryLease(ArrayPool<T> arrayPool, T[] array, int length) {
            _arrayPool = arrayPool;
            _array = array;
            _length = length;
        }

        public Memory<T> AsMemory() => _array.AsMemory(0, _length);
        public Span<T> AsSpan() => _array.AsSpan(0, _length);

        public void Dispose() {
            // can be null if created through default constructor
            if (_array != null)
                _arrayPool.Return(_array);
        }
    }
}