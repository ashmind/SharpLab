using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using MirrorSharp.Advanced;
using MirrorSharp.FSharp.Advanced;
using SharpLab.Server.MirrorSharp.Internal;

namespace SharpLab.Server.MirrorSharp {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class SetOptionsFromClient : ISetOptionsFromClientExtension {
        private const string Target = "x-target";

        public bool TrySetOption(IWorkSession session, string name, string value) {
            if (name != Target && name != "x-target-language" /* TODO: remove once all branches and main are updated */)
                return false;

            session.SetTargetName(value);
            SetAllowUnsafe(session, value != TargetNames.Run);
            return true;
        }

        private void SetAllowUnsafe(IWorkSession session, bool enabled) {
            if (!session.IsRoslyn)
                return;
            var project = session.Roslyn.Project;
            if (!(project.CompilationOptions is CSharpCompilationOptions csharpOptions))
                return;
            project = project.WithCompilationOptions(csharpOptions.WithAllowUnsafe(enabled));
            session.Roslyn.Project = project;
        }
    }
}