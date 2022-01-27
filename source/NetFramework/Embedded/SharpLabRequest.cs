using System;
using System.IO;

namespace SharpLab.Embedded {
    public class SharpLabRequest {
        public SharpLabRequest(
            Stream assemblyStream,
            Stream? symbolStream,
            string reflectionTypeName,
            SharpLabTarget target,
            TextWriter output
        ) {
            Argument.NotNull(nameof(assemblyStream), assemblyStream);
            Argument.NotNullOrEmpty(nameof(reflectionTypeName), reflectionTypeName);
            Argument.NotNull(nameof(output), output);
            if (target is not (SharpLabTarget.JitAsm or SharpLabTarget.IL or SharpLabTarget.CSharp or SharpLabTarget.Ast))
                throw new ArgumentOutOfRangeException(nameof(target), target, $"Specified target is not supported. (Value {target})");

            AssemblyStream = assemblyStream;
            SymbolStream = symbolStream;
            ReflectionTypeName = reflectionTypeName;
            Target = target;
            Output = output;
        }

        public Stream AssemblyStream { get; }
        public Stream? SymbolStream { get; }
        public string ReflectionTypeName { get; }
        public SharpLabTarget Target { get; }
        public TextWriter Output { get; }
    }
}
