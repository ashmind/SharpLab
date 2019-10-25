using System.Collections.Immutable;

namespace SharpLab.Server.Common.Internal {
    internal static class PreprocessorSymbols {
        public static readonly ImmutableArray<string> Release = ImmutableArray.Create("NETCOREAPP", "NETCOREAPP3_0");
        public static readonly ImmutableArray<string> Debug = Release.Add("DEBUG");
    }
}
