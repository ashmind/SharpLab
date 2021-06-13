using System.Threading.Tasks;

namespace SharpLab.Server.Common {
    public static class ValueTaskExtensions {
        // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
        public static ValueTask AsUntypedValueTask<T>(this ValueTask<T> valueTask) {
            if (valueTask.IsCompletedSuccessfully) {
                _ = valueTask.Result;
                return default;
            }

            return new ValueTask(valueTask.AsTask());
        }
    }
}
