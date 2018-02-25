using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.VisualBasic.CompilerServices;
using AshMind.Extensions;
using Unbreakable;
using Unbreakable.Policy;
using Unbreakable.Policy.Rewriters;
using SharpLab.Runtime.Internal;

namespace SharpLab.Server.Execution.Unbreakable {
    using static ApiAccess;

    public static class ApiPolicySetup {
        public static ApiPolicy CreatePolicy() => ApiPolicy.SafeDefault()
            .Namespace("System", Neutral, SetupSystem)
            .Namespace("System.Collections.Concurrent", Neutral, SetupSystemCollectionsConcurrent)
            .Namespace("System.Diagnostics", Neutral, SetupSystemDiagnostics)
            .Namespace("System.Globalization", Neutral, SetupSystemGlobalization)
            .Namespace("System.Reflection", Neutral, SetupSystemReflection)
            .Namespace("System.Linq.Expressions", Neutral, SetupSystemLinqExpressions)
            .Namespace("System.Net", Neutral, SetupSystemNet)
            .Namespace("System.Numerics", Neutral, SetupSystemNumerics)
            .Namespace("System.IO", Neutral,
                // required by F#'s printf
                n => n.Type(typeof(TextWriter), Neutral)
            )
            .Namespace("System.Security.Cryptography", Neutral, SetupSystemSecurityCryptography)
            .Namespace("System.Text", Neutral, SetupSystemText)
            .Namespace("System.Web", Neutral, SetupSystemWeb)
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
            .Namespace("Microsoft.FSharp.Core", Neutral, SetupFSharpCore)
            .Namespace("Microsoft.FSharp.Collections", Neutral,
                n => n.Type(typeof(FSharpList<>), Allowed)
                      .Type(typeof(ListModule), Neutral,
                          t => t.Member(nameof(ListModule.Iterate), Allowed)
                      )
                      .Type(typeof(SeqModule), Neutral,
                          t => t.Member(nameof(SeqModule.ToArray), Allowed, CollectedEnumerableArgumentRewriter.Default)
                                .Member(nameof(SeqModule.ToList), Allowed, CollectedEnumerableArgumentRewriter.Default)
                      )
            )
            .Namespace("Microsoft.VisualBasic", Neutral, SetupMicrosoftVisualBasic)
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

        private static void SetupSystem(NamespacePolicy namespacePolicy) {
            namespacePolicy
                .Type(typeof(BitConverter), Neutral,
                    t => t.Member(nameof(BitConverter.GetBytes), Allowed, ArrayReturnRewriter.Default)
                )
                .Type(typeof(Console), Neutral,
                    t => t.Member(nameof(Console.Write), Allowed)
                          .Member(nameof(Console.WriteLine), Allowed)
                          // required by F#'s printf
                          .Getter(nameof(Console.Out), Allowed)
                )
                .Type(typeof(ReadOnlySpan<>), Allowed,
                    t => t.Member(nameof(ReadOnlySpan<object>.DangerousCreate), Denied)
                )
                .Type(typeof(ReadOnlySpan<>.Enumerator), Allowed)
                .Type(typeof(Span<>), Allowed,
                    t => t.Member(nameof(ReadOnlySpan<object>.DangerousCreate), Denied)
                )
                .Type(typeof(Span<>.Enumerator), Allowed)
                .Type(typeof(STAThreadAttribute), Allowed)
                .Type(typeof(NotImplementedException), Neutral, t => t.Constructor(Allowed))
                .Type(typeof(Type), Neutral, SetupSystemType);
        }

        private static void SetupSystemType(TypePolicy typePolicy) {
            typePolicy
                .Member(nameof(Type.GetConstructor), Allowed)
                .Member(nameof(Type.GetEvent), Allowed)
                .Member(nameof(Type.GetField), Allowed)
                .Member(nameof(Type.GetInterface), Allowed)
                .Member(nameof(Type.GetMethod), Allowed)
                .Member(nameof(Type.GetProperty), Allowed);
        }

        private static void SetupSystemCollectionsConcurrent(NamespacePolicy namespacePolicy) {
            namespacePolicy.Type(typeof(ConcurrentDictionary<,>), Allowed,
                t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity)
                      .Member(nameof(ConcurrentDictionary<object, object>.AddOrUpdate), Allowed, AddCallRewriter.Default)
                      .Member(nameof(ConcurrentDictionary<object, object>.GetOrAdd), Allowed, AddCallRewriter.Default)
                      .Member(nameof(ConcurrentDictionary<object, object>.TryAdd), Allowed, AddCallRewriter.Default)
            );
        }

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

        private static void SetupSystemGlobalization(NamespacePolicy namespacePolicy) {
            namespacePolicy
                .Type(typeof(CultureInfo), Neutral, typePolicy => {
                    typePolicy.Constructor(Allowed)
                              .Member(nameof(CultureInfo.GetCultureInfo), Allowed)
                              .Member(nameof(CultureInfo.GetCultureInfoByIetfLanguageTag), Allowed);
                    foreach (var property in typeof(CultureInfo).GetProperties()) {
                        typePolicy.Getter(property.Name, Allowed);
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

        private static void SetupSystemNet(NamespacePolicy namespacePolicy) {
            namespacePolicy
                .Type(typeof(IPAddress), Allowed);
        }

        private static void SetupSystemNumerics(NamespacePolicy namespacePolicy) {
            namespacePolicy
                .Type(typeof(Complex), Allowed);
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

        private static void SetupSystemSecurityCryptography(NamespacePolicy namespacePolicy) {
            ForEachTypeInNamespaceOf<HashAlgorithm>(type => {
                if (!type.IsSameAsOrSubclassOf<HashAlgorithm>())
                    return;

                namespacePolicy.Type(type, Neutral,
                    t => t.Constructor(Allowed, DisposableReturnRewriter.Default)
                          .Member(nameof(HashAlgorithm.ComputeHash), Allowed, ArrayReturnRewriter.Default)
                );
            });
        }

        private static void SetupSystemText(NamespacePolicy namespacePolicy) {
            namespacePolicy
                .Type(typeof(Encoding), Neutral,
                    // TODO: Move to Unbreakable
                    t => t.Member(nameof(Encoding.GetBytes), Allowed, ArrayReturnRewriter.Default)
                );
        }

        private static void SetupSystemWeb(NamespacePolicy namespacePolicy) {
            namespacePolicy
                .Type(typeof(HttpServerUtility), Neutral,
                    t => t.Member(nameof(HttpServerUtility.HtmlDecode), Allowed)
                          .Member(nameof(HttpServerUtility.HtmlEncode), Allowed).Member(nameof(HttpServerUtility.UrlDecode), Allowed)
                          .Member(nameof(HttpServerUtility.UrlEncode), Allowed)
                          .Member(nameof(HttpServerUtility.UrlTokenDecode), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(HttpServerUtility.UrlTokenEncode), Allowed)
                )
                .Type(typeof(HttpUtility), Neutral,
                    t => t.Member(nameof(HttpUtility.HtmlDecode), Allowed)
                          .Member(nameof(HttpUtility.HtmlEncode), Allowed)
                          .Member(nameof(HttpUtility.UrlDecode), Allowed)
                          .Member(nameof(HttpUtility.UrlEncode), Allowed)
                          .Member(nameof(HttpUtility.HtmlAttributeEncode), Allowed)
                          .Member(nameof(HttpUtility.JavaScriptStringEncode), Allowed)
                );
        }

        private static void SetupFSharpCore(NamespacePolicy namespacePolicy) {
            namespacePolicy
                .Type(typeof(CompilationArgumentCountsAttribute), Allowed)
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
                .Type(typeof(LanguagePrimitives), Neutral,
                    t => t.Getter(nameof(LanguagePrimitives.GenericComparer), Allowed)
                          .Getter(nameof(LanguagePrimitives.GenericEqualityComparer), Allowed)
                          .Getter(nameof(LanguagePrimitives.GenericEqualityERComparer), Allowed)
                )
                .Type(typeof(OptimizedClosures.FSharpFunc<,,>), Allowed)
                .Type(typeof(OptimizedClosures.FSharpFunc<,,,>), Allowed)
                .Type(typeof(OptimizedClosures.FSharpFunc<,,,,>), Allowed)
                .Type(typeof(Microsoft.FSharp.Core.Operators), Allowed,
                    t => t.Member("ConsoleError", Denied)
                          .Member("ConsoleIn", Denied)
                          .Member("ConsoleOut", Denied)
                          .Member("Lock", Denied)
                )
                .Type(typeof(Microsoft.FSharp.Core.Operators.OperatorIntrinsics), Neutral,
                    t => t.Member("RangeInt32", Allowed)
                )
                .Type(typeof(PrintfFormat<,,,>), Allowed)
                .Type(typeof(PrintfFormat<,,,,>), Allowed)
                .Type(typeof(PrintfModule), Neutral,
                    t => t.Member(nameof(PrintfModule.PrintFormat), Allowed)
                          .Member(nameof(PrintfModule.PrintFormatLine), Allowed)
                          .Member(nameof(PrintfModule.PrintFormatToTextWriter), Allowed)
                          .Member(nameof(PrintfModule.PrintFormatLineToTextWriter), Allowed)
                )
                .Type(typeof(Unit), Allowed);
        }

        private static void SetupMicrosoftVisualBasic(NamespacePolicy namespacePolicy) {
            namespacePolicy.Type(nameof(Microsoft.VisualBasic.Strings), Allowed);
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
