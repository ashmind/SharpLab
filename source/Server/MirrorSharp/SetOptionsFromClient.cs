using JetBrains.Annotations;
using MirrorSharp.Advanced;

namespace SharpLab.Server.MirrorSharp {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class SetOptionsFromClient : ISetOptionsFromClientExtension {
        private const string Target = "x-target";

        public bool TrySetOption(IWorkSession session, string name, string value) {
            if (name != Target && name != "x-target-language" /* TODO: remove once all branches and main are updated */)
                return false;

            session.SetTargetName(value);
            return true;
        }
    }
}