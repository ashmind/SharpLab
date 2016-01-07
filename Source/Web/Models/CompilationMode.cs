using JetBrains.Annotations;

namespace TryRoslyn.Web.Models {
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public enum CompilationMode {
        Regular,
        Script
    }
}