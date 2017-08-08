using System;
using System.IO;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.VisualBasic.CompilerServices;
using SharpLab.Runtime.Internal;
using Unbreakable;
using Unbreakable.Rules.Rewriters;

namespace SharpLab.Server.Execution {
    public static class ApiRulesSetup {
        public static ApiRules CreateRules() => ApiRules.SafeDefaults()
            .Namespace("System", ApiAccess.Neutral,
                n => n.Type(typeof(Console), ApiAccess.Neutral,
                        t => t.Member(nameof(Console.Write), ApiAccess.Allowed)
                              .Member(nameof(Console.WriteLine), ApiAccess.Allowed)
                              // required by F#'s printf
                              .Getter(nameof(Console.Out), ApiAccess.Allowed)
                     ).Type(typeof(STAThreadAttribute), ApiAccess.Allowed)
            )
            .Namespace("System.IO", ApiAccess.Neutral,
                // required by F#'s printf
                n => n.Type(typeof(TextWriter), ApiAccess.Neutral)
            )
            .Namespace("SharpLab.Runtime.Internal", ApiAccess.Neutral,
                n => n.Type(typeof(Flow), ApiAccess.Neutral,
                         t => t.Member(nameof(Flow.ReportException), ApiAccess.Allowed, NoGuardRewriter.Default)
                               .Member(nameof(Flow.ReportLineStart), ApiAccess.Allowed, NoGuardRewriter.Default)
                               .Member(nameof(Flow.ReportValue), ApiAccess.Allowed, NoGuardRewriter.Default)
                     )
            )
            .Namespace("", ApiAccess.Neutral,
                n => n.Type(typeof(SharpLabObjectExtensions), ApiAccess.Allowed)
            )
            .Namespace("Microsoft.FSharp.Core", ApiAccess.Neutral,
                n => n.Type(typeof(CompilationArgumentCountsAttribute), ApiAccess.Allowed)
                      .Type(typeof(CompilationMappingAttribute), ApiAccess.Allowed)
                      .Type(typeof(EntryPointAttribute), ApiAccess.Allowed)
                      .Type(typeof(FSharpChoice<,>), ApiAccess.Allowed)
                      .Type(typeof(FSharpFunc<,>), ApiAccess.Allowed)
                      .Type(typeof(FSharpOption<>), ApiAccess.Allowed)
                      .Type(typeof(OptimizedClosures.FSharpFunc<,,>), ApiAccess.Allowed)
                      .Type(typeof(OptimizedClosures.FSharpFunc<,,,>), ApiAccess.Allowed)
                      .Type(typeof(OptimizedClosures.FSharpFunc<,,,,>), ApiAccess.Allowed)
                      .Type(typeof(PrintfFormat<,,,>), ApiAccess.Allowed)
                      .Type(typeof(PrintfFormat<,,,,>), ApiAccess.Allowed)
                      .Type(typeof(PrintfModule), ApiAccess.Neutral,
                          t => t.Member(nameof(PrintfModule.PrintFormat), ApiAccess.Allowed)
                                .Member(nameof(PrintfModule.PrintFormatLine), ApiAccess.Allowed)
                                .Member(nameof(PrintfModule.PrintFormatToTextWriter), ApiAccess.Allowed)
                                .Member(nameof(PrintfModule.PrintFormatLineToTextWriter), ApiAccess.Allowed)
                        )
                        .Type(typeof(Unit), ApiAccess.Allowed)
            )
            .Namespace("Microsoft.FSharp.Collections", ApiAccess.Neutral,
                n => n.Type(typeof(FSharpList<>), ApiAccess.Allowed)
            )
            .Namespace("Microsoft.VisualBasic.CompilerServices", ApiAccess.Neutral,
                n => n.Type(typeof(StandardModuleAttribute), ApiAccess.Allowed)
            );
    }
}
