using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace SharpLab.Server.Common.Internal {
    internal static class PreprocessorSymbols {
        public static readonly ImmutableArray<string> Release = ImmutableArray.Create("NETCOREAPP", "NETCOREAPP3_0");
        public static readonly ImmutableArray<string> Debug = Release.Add("DEBUG");
    }
}
