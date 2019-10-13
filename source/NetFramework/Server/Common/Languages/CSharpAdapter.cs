using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MirrorSharp;
using MirrorSharp.Advanced;
using SharpLab.Runtime;
using SharpLab.Server.Common.Internal;
using SharpLab.Server.Compilation.Internal;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace SharpLab.Server.Common.Languages {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class CSharpAdapter : ILanguageAdapter {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof (LanguageVersion))
            .Cast<LanguageVersion>()
            .Where(v => v != LanguageVersion.Latest) // seems like latest got fixed at some point
            .Max();
        private static readonly ImmutableArray<string> ReleasePreprocessorSymbols = ImmutableArray.Create("__DEMO_EXPERIMENTAL__", "NETFRAMEWORK");
        private static readonly ImmutableArray<string> DebugPreprocessorSymbols = ReleasePreprocessorSymbols.Add("DEBUG");

        private readonly ImmutableList<MetadataReference> _references;

        public CSharpAdapter(IAssemblyReferenceCollector referenceCollector, IAssemblyDocumentationResolver documentationResolver) {
            var referencedAssemblies = referenceCollector.SlowGetAllReferencedAssembliesRecursive(
                // Essential
                NetFrameworkRuntime.AssemblyOfValueTask,
                NetFrameworkRuntime.AssemblyOfValueTuple,
                NetFrameworkRuntime.AssemblyOfSpan,
                typeof(Binder).Assembly,

                // Runtime
                typeof(JitGenericAttribute).Assembly,

                // Requested
                typeof(XDocument).Assembly, // System.Xml.Linq
                typeof(IDataReader).Assembly, // System.Data
                typeof(HttpUtility).Assembly // System.Web
            ).ToImmutableList();

            var assemblyReferenceTaskSource = new AssemblyReferenceDiscoveryTaskSource();
            assemblyReferenceTaskSource.Complete(referencedAssemblies.Select(a => a.Location).ToImmutableList());
            AssemblyReferenceDiscoveryTask = assemblyReferenceTaskSource.Task;

            _references = referencedAssemblies
                .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location, documentation: documentationResolver.GetDocumentation(a)))
                .ToImmutableList();
        }

        public string LanguageName => LanguageNames.CSharp;
        public AssemblyReferenceDiscoveryTask AssemblyReferenceDiscoveryTask { get; }

        public void SlowSetup(MirrorSharpOptions options) {
            // ReSharper disable HeapView.ObjectAllocation.Evident

            options.CSharp.ParseOptions = new CSharpParseOptions(
                MaxLanguageVersion,
                preprocessorSymbols: DebugPreprocessorSymbols,
                documentationMode: DocumentationMode.Diagnose
            );
            options.CSharp.CompilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                specificDiagnosticOptions: new Dictionary<string, ReportDiagnostic> {
                    // CS1591: Missing XML comment for publicly visible type or member
                    { "CS1591", ReportDiagnostic.Suppress }
                }
            );
            options.CSharp.MetadataReferences = _references;

            // ReSharper restore HeapView.ObjectAllocation.Evident
        }

        public void SetOptimize(IWorkSession session, string optimize) {
            var project = session.Roslyn.Project;
            var parseOptions = ((CSharpParseOptions)project.ParseOptions!);
            var compilationOptions = ((CSharpCompilationOptions)project.CompilationOptions!);
            session.Roslyn.Project = project
                .WithParseOptions(parseOptions.WithPreprocessorSymbols(optimize == Optimize.Debug ? DebugPreprocessorSymbols : ReleasePreprocessorSymbols))
                .WithCompilationOptions(compilationOptions.WithOptimizationLevel(optimize == Optimize.Debug ? OptimizationLevel.Debug : OptimizationLevel.Release));
        }

        public void SetOptionsForTarget(IWorkSession session, string target) {
            var outputKind = target != TargetNames.Run ? OutputKind.DynamicallyLinkedLibrary : OutputKind.ConsoleApplication;
            var allowUnsafe = target != TargetNames.Run;

            var project = session.Roslyn.Project;
            var options = ((CSharpCompilationOptions)project.CompilationOptions!);
            session.Roslyn.Project = project.WithCompilationOptions(
                options.WithOutputKind(outputKind).WithAllowUnsafe(allowUnsafe)
            );
        }

        public ImmutableArray<int> GetMethodParameterLines(IWorkSession session, int lineInMethod, int columnInMethod) {
            var declaration = RoslynAdapterHelper.FindSyntaxNodeInSession(session, lineInMethod, columnInMethod)
                ?.AncestorsAndSelf()
                .FirstOrDefault(m => m is MemberDeclarationSyntax
                                  || m is AnonymousFunctionExpressionSyntax
                                  || m is LocalFunctionStatementSyntax);

            var parameters = declaration switch {
                BaseMethodDeclarationSyntax m => m.ParameterList.Parameters,
                ParenthesizedLambdaExpressionSyntax l => l.ParameterList.Parameters,
                SimpleLambdaExpressionSyntax l => SyntaxFactory.SingletonSeparatedList(l.Parameter),
                LocalFunctionStatementSyntax f => f.ParameterList.Parameters,
                _ => SyntaxFactory.SeparatedList<ParameterSyntax>()
            };

            if (parameters.Count == 0)
                return ImmutableArray<int>.Empty;

            var results = new int[parameters.Count];
            for (var i = 0; i < parameters.Count; i++) {
                results[i] = parameters[i].GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            }
            return ImmutableArray.Create(results);
        }

        public ImmutableArray<string?> GetCallArgumentIdentifiers([NotNull] IWorkSession session, int callStartLine, int callStartColumn) {
            var call = RoslynAdapterHelper.FindSyntaxNodeInSession(session, callStartLine, callStartColumn)
                ?.AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();
            if (call == null)
                return ImmutableArray<string?>.Empty;

            var arguments = call.ArgumentList.Arguments;
            if (arguments.Count == 0)
                return ImmutableArray<string?>.Empty;

            var results = new string?[arguments.Count];
            for (var i = 0; i < arguments.Count; i++) {
                results[i] = (arguments[i].Expression is IdentifierNameSyntax n) ? n.Identifier.ValueText : null;
            }
            return ImmutableArray.Create(results);
        }
    }
}
