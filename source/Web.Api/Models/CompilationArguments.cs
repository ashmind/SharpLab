using JetBrains.Annotations;
using Newtonsoft.Json;
using TryRoslyn.Core;

namespace TryRoslyn.Web.Api.Models {
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class CompilationArguments {
        public string Code { get; set; }
        public CompilationMode Mode { get; set; }
        [JsonProperty("language")]
        public LanguageIdentifier SourceLanguage { get; set; }
        [JsonProperty("target")]
        public LanguageIdentifier TargetLanguage { get; set; }
        [JsonProperty("optimizations")]
        public bool OptimizationsEnabled { get; set; }
    }
}