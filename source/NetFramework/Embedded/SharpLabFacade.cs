using System.Collections.Generic;
using SharpLab.Embedded.Internal;
using SharpLab.Embedded.Internal.Targets;
using SharpLab.Server.Decompilation;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Embedded {
    public static class SharpLabFacade {
        private static readonly IReadOnlyDictionary<SharpLabTarget, ITarget> _targets = new Dictionary<SharpLabTarget, ITarget> {
            { SharpLabTarget.CSharp, new DecompilerTarget(new CSharpDecompiler(
                new EmbeddedAssemblyResolver(),
                stream => new PortablePdbDebugInfoProvider(stream)
              )) }
        };

        public static void Decompile(SharpLabRequest request) {
            Argument.NotNull(nameof(request), request);
            var target = _targets[request.Target];
            target.Decompile(request);
        }
    }
}
