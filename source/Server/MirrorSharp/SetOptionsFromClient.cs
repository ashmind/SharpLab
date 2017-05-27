using JetBrains.Annotations;
using MirrorSharp.Advanced;

namespace SharpLab.Server.MirrorSharp {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class SetOptionsFromClient : ISetOptionsFromClientExtension {
        private const string Target = "x-target";

        public bool TrySetOption(IWorkSession session, string name, string value) {
            if (name != Target)
                return false;

            session.SetTargetName(value);
            return true;
        }
    }
}