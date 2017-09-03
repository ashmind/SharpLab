using System;
using System.Diagnostics;
using System.IO;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.VisualBasic.CompilerServices;
using System.Linq.Expressions;
using System.Reflection;
using AshMind.Extensions;
using SharpLab.Runtime.Internal;
using Unbreakable;
using Unbreakable.Policy;
using Unbreakable.Policy.Rewriters;

namespace SharpLab.Server.Execution.Internal {
    using static ApiAccess;

    public static class ApiPolicySetup {
        public static ApiPolicy CreatePolicy() => ApiPolicy.SafeDefault()
            .Namespace("System", Neutral,
                n => n.Type(typeof(Console), Neutral,
                        t => t.Member(nameof(Console.Write), Allowed)
                              .Member(nameof(Console.WriteLine), Allowed)
                              // required by F#'s printf
                              .Getter(nameof(Console.Out), Allowed)
                     ).Type(typeof(STAThreadAttribute), Allowed)
                      .Type(typeof(NotImplementedException), Neutral, t => t.Constructor(Allowed))
            )
            .Namespace("System.Diagnostics", Neutral, SetupSystemDiagnostics)
            .Namespace("System.Reflection", Neutral, SetupSystemReflection)
            .Namespace("System.Linq.Expressions", Neutral, SetupSystemLinqExpressions)
            .Namespace("System.IO", Neutral,
                // required by F#'s printf
                n => n.Type(typeof(TextWriter), Neutral)
            )
            .Namespace("SharpLab.Runtime.Internal", Neutral,
                n => n.Type(typeof(Flow), Neutral,
                         t => t.Member(nameof(Flow.ReportException), Allowed, NoGuardRewriter.Default)
                               .Member(nameof(Flow.ReportLineStart), Allowed, NoGuardRewriter.Default)
                               .Member(nameof(Flow.ReportValue), Allowed, NoGuardRewriter.Default)
                     )
            )
            .Namespace("", Neutral,
                n => n.Type(typeof(SharpLabObjectExtensions), Allowed)
            )
            .Namespace("Microsoft.FSharp.Core", Neutral,
                n => n.Type(typeof(CompilationArgumentCountsAttribute), Allowed)
                      .Type(typeof(CompilationMappingAttribute), Allowed)
                      .Type(typeof(EntryPointAttribute), Allowed)
                      .Type(typeof(ExtraTopLevelOperators), Neutral,
                          t => t.Member(nameof(ExtraTopLevelOperators.CreateDictionary), Allowed, CollectedEnumerableArgumentRewriter.Default)
                                .Member(nameof(ExtraTopLevelOperators.CreateSet), Allowed, CollectedEnumerableArgumentRewriter.Default)
                                .Member(nameof(ExtraTopLevelOperators.LazyPattern), Allowed)
                                .Member(nameof(ExtraTopLevelOperators.PrintFormat), Allowed)
                                .Member(nameof(ExtraTopLevelOperators.PrintFormatLine), Allowed)
                                .Member(nameof(ExtraTopLevelOperators.PrintFormatToTextWriter), Allowed)
                                .Member(nameof(ExtraTopLevelOperators.PrintFormatLineToTextWriter), Allowed)
                                .Member(nameof(ExtraTopLevelOperators.PrintFormatToString), Allowed)
                                .Member(nameof(ExtraTopLevelOperators.SpliceExpression), Allowed)
                                .Member(nameof(ExtraTopLevelOperators.SpliceUntypedExpression), Allowed)
                                .Member(nameof(ExtraTopLevelOperators.ToByte), Allowed)
                                .Member(nameof(ExtraTopLevelOperators.ToDouble), Allowed)
                                .Member(nameof(ExtraTopLevelOperators.ToSByte), Allowed)
                                .Member(nameof(ExtraTopLevelOperators.ToSingle), Allowed)
                      )
                      .Type(typeof(ExtraTopLevelOperators.Checked), Allowed)
                      .Type(typeof(FSharpChoice<,>), Allowed)
                      .Type(typeof(FSharpFunc<,>), Allowed)
                      .Type(typeof(FSharpOption<>), Allowed)
                      .Type(typeof(OptimizedClosures.FSharpFunc<,,>), Allowed)
                      .Type(typeof(OptimizedClosures.FSharpFunc<,,,>), Allowed)
                      .Type(typeof(OptimizedClosures.FSharpFunc<,,,,>), Allowed)
                      .Type(typeof(Microsoft.FSharp.Core.Operators), Allowed,
                          t => t.Member("ConsoleError", Denied)
                                .Member("ConsoleIn", Denied)
                                .Member("ConsoleOut", Denied)
                                .Member("Lock", Denied)
                      )
                      .Type(typeof(PrintfFormat<,,,>), Allowed)
                      .Type(typeof(PrintfFormat<,,,,>), Allowed)
                      .Type(typeof(PrintfModule), Neutral,
                          t => t.Member(nameof(PrintfModule.PrintFormat), Allowed)
                                .Member(nameof(PrintfModule.PrintFormatLine), Allowed)
                                .Member(nameof(PrintfModule.PrintFormatToTextWriter), Allowed)
                                .Member(nameof(PrintfModule.PrintFormatLineToTextWriter), Allowed)
                        )
                        .Type(typeof(Unit), Allowed)
            )
            .Namespace("Microsoft.FSharp.Collections", Neutral,
                n => n.Type(typeof(FSharpList<>), Allowed)
            )
            .Namespace("Microsoft.VisualBasic.CompilerServices", Neutral,
                n => n.Type(typeof(Conversions), Allowed,
                        t => t.Member(nameof(Conversions.FromCharAndCount), Allowed, new CountArgumentRewriter("Count"))
                              // Those need extra review
                              .Member(nameof(Conversions.ChangeType), Denied)
                              .Member(nameof(Conversions.FallbackUserDefinedConversion), Denied)
                              .Member(nameof(Conversions.FromCharArray), Denied)
                              .Member(nameof(Conversions.FromCharArraySubset), Denied)
                              .Member(nameof(Conversions.ToCharArrayRankOne), Denied)
                      )
                      .Type(typeof(StandardModuleAttribute), Allowed)
            );

        private static void SetupSystemDiagnostics(NamespacePolicy namespacePolicy) {
            namespacePolicy
                .Type(typeof(Stopwatch), Allowed, typePolicy => {
                    foreach (var property in typeof(Stopwatch).GetProperties()) {
                        if (!property.Name.Contains("Elapsed"))
                            continue;
                        typePolicy.Getter(property.Name, Allowed, new WarningMemberRewriter(
                            "Please do not rely on Stopwatch results in SharpLab.\r\n\r\n" +
                            "There are many checks and reports added to your code before it runs,\r\n" +
                            "so the performance might be completely unrelated to the original code."
                        ));
                    }
                });
        }

        private static void SetupSystemLinqExpressions(NamespacePolicy namespacePolicy) {
            ForEachTypeInNamespaceOf<Expression>(type => {
                if (type.IsEnum) {
                    namespacePolicy.Type(type, Allowed);
                    return;
                }

                if (!type.IsSameAsOrSubclassOf<Expression>())
                    return;

                namespacePolicy.Type(type, Allowed, typePolicy => {
                    foreach (var method in type.GetMethods()) {
                        if (method.Name.Contains("Compile"))
                            typePolicy.Member(method.Name, Denied);
                    }
                });
            });
        }

        private static void SetupSystemReflection(NamespacePolicy namespacePolicy) {
            ForEachTypeInNamespaceOf<MemberInfo>(type => {
                if (type.IsEnum) {
                    namespacePolicy.Type(type, Allowed);
                    return;
                }

                if (!type.IsSameAsOrSubclassOf<MemberInfo>())
                    return;

                namespacePolicy.Type(type, Neutral, typePolicy => {
                    foreach (var property in type.GetProperties()) {
                        if (property.Name.Contains("Handle"))
                            continue;
                        typePolicy.Getter(property.Name, Allowed);
                    }
                    foreach (var method in type.GetMethods()) {
                        if (method.ReturnType.IsSameAsOrSubclassOf<MemberInfo>())
                            typePolicy.Member(method.Name, Allowed);
                    }
                });
            });
        }

        private static void ForEachTypeInNamespaceOf<T>(Action<Type> action) {
            var types = typeof(T).Assembly.GetExportedTypes();
            foreach (var type in types) {
                if (type.Namespace != typeof(T).Namespace)
                    continue;

                action(type);
            }
        }
    }
}
