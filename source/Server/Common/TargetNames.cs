using System.Collections.Immutable;

namespace SharpLab.Server.Common;

public static class TargetNames {
    public const string CSharp = LanguageNames.CSharp;
    public const string IL = LanguageNames.IL;
    public const string Ast = "AST";
    public const string JitAsm = "JIT ASM";
    public const string Run = "Run";
    public const string RunIL = "Run IL";
    public const string Verify = "Verify";
    public const string Explain = "Explain";

    public static readonly ImmutableArray<string> All = [
        CSharp, IL, Ast, JitAsm, Run, RunIL, Verify, Explain
    ];
}