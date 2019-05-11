using System.IO;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Runtime;
using SharpLab.Server.AspNetCore.Platform;
using SharpLab.Server.Decompilation;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.AspNetCore.Decompilation {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class JitAsmDecompiler : JitAsmDecompilerBase {
        protected override JitAsmResultScope JitCompileAndGetMethods(MemoryStream assemblyStream) {
            CustomAssemblyLoadContext? context = null;
            try {
                context = new CustomAssemblyLoadContext(shouldShareAssembly: _ => true);
                var assembly = context.LoadFromStream(assemblyStream);
                var results = IsolatedJitAsmDecompilerCore.JitCompileAndGetMethods(assembly);
                return new JitAsmResultScope(results, context);
            }
            catch {
                context?.Dispose();
                throw;
            }
        }

        protected override ClrFlavor ClrFlavor => ClrFlavor.Core;
    }
}