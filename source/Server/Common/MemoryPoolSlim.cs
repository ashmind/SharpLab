using System.Buffers;

namespace SharpLab.Server.Common {
    public class MemoryPoolSlim<T> {
        private readonly ArrayPool<T> _arrayPool;

        public static MemoryPoolSlim<T> Shared { get; } = new MemoryPoolSlim<T>(ArrayPool<T>.Shared);

        public MemoryPoolSlim(ArrayPool<T> arrayPool) {
            _arrayPool = arrayPool;
        }

        public MemoryLease<T> RentExact(int length) {
            var array = (T[]?)null;
            try {
                array = _arrayPool.Rent(length);
                return new(_arrayPool, array, length);
            }
            catch {
                if (array != null)
                    _arrayPool.Return(array);
                throw;
            }
        }
    }
}
