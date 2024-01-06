using System.Collections.Immutable;

namespace SharpLab.Server.Common;

public static class TargetNames {
    public const string CSharp = LanguageNames.CSharp;
    public const string IL = "IL";
    public const string Ast = "AST";
    public const string JitAsm = "JIT ASM";
    public const string Run = "Run";
    public const string Verify = "Verify";
    public const string Explain = "Explain";

    public static readonly ImmutableArray<string> All = ImmutableArray.Create(
        CSharp, IL, Ast, JitAsm, Run, Verify, Explain
    );
}