using System.Collections.Immutable;
using CodeAnalysis = Microsoft.CodeAnalysis;

namespace SharpLab.Server.Common;

public class LanguageNames {
    public const string CSharp = CodeAnalysis.LanguageNames.CSharp;
    public const string VisualBasic = CodeAnalysis.LanguageNames.VisualBasic;
    public const string FSharp = CodeAnalysis.LanguageNames.FSharp;
    public const string IL = "IL";

    public static readonly ImmutableArray<string> All = [
        CSharp, VisualBasic, FSharp, IL
    ];
}
