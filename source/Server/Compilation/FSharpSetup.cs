using JetBrains.Annotations;
using MirrorSharp;
using MirrorSharp.FSharp;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Compilation {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class FSharpSetup : IMirrorSharpSetup {
        public void ApplyTo(MirrorSharpOptions options) {
            options.EnableFSharp();
        }
    }
}