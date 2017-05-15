using JetBrains.Annotations;
using MirrorSharp;
using MirrorSharp.FSharp;
using TryRoslyn.Server.MirrorSharp;

namespace TryRoslyn.Server.Compilation {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class FSharpSetup : IMirrorSharpSetup {
        public void ApplyTo(MirrorSharpOptions options) {
            options.EnableFSharp();
        }
    }
}