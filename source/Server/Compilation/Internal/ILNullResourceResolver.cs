using System;
using Mobius.ILasm.interfaces;

namespace SharpLab.Server.Compilation.Internal {
    public class ILNullResourceResolver : IManifestResourceResolver {
        public static ILNullResourceResolver Default { get; } = new();

        public bool TryGetResourceBytes(string path, out byte[] bytes, out string error) {
            bytes = Array.Empty<byte>();
            error = $"Resource file '{path}' does not exist or cannot be accessed from SharpLab";
            return false;
        }
    }
}
