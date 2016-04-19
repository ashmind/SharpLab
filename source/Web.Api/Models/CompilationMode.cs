using JetBrains.Annotations;

namespace TryRoslyn.Web.Api.Models {
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public enum CompilationMode {
        Regular,
        Script
    }
}