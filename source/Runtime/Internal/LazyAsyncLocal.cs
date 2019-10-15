using System;
using System.Threading;

namespace SharpLab.Runtime.Internal {
    internal class LazyAsyncLocal<T>
        where T : class
    {
        private readonly Func<T> _createValue;
        private AsyncLocal<T?> _value = new AsyncLocal<T?>();

        public LazyAsyncLocal(Func<T> createValue) {
            _createValue = createValue;
        }

        public T Value {
            get {
                var value = _value.Value;
                if (value == null) {
                    value = _createValue();
                    _value.Value = value;
                }
                return value;
            }
        }

        public T? ValueIfCreated => _value.Value;
    }
}
