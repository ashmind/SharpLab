using JetBrains.Annotations;

namespace TryRoslyn.Web.Models {
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class CompilationArguments {
        public string Code { get; set; }
        public CompilationMode Mode { get; set; }
        public string Branch { get; set; }
    }
}