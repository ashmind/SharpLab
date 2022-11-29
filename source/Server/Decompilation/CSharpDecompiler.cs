using System;
using System.IO;
using System.Runtime.CompilerServices;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Metadata;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    public class CSharpDecompiler : IDecompiler {
        private static readonly CSharpFormattingOptions FormattingOptions = CreateFormattingOptions();
        private static readonly DecompilerSettings DecompilerSettings = new(ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp1) {
            ArrayInitializers = false,
            AutomaticEvents = false,
            DecimalConstants = false,
            FixedBuffers = false,
            UsingStatement = false,
            SwitchStatementOnString = false,
            LockStatement = false,
            ForStatement = false,
            ForEachStatement = false,
            SparseIntegerSwitch = false,
            DoWhileStatement = false,
            StringConcat = false,
            UseRefLocalsForAccurateOrderOfEvaluation = true,
            InitAccessors = true,
            FunctionPointers = true,
            NativeIntegers = true
        };

        private readonly IAssemblyResolver _assemblyResolver;
        private readonly Func<Stream, IDisposableDebugInfoProvider> _debugInfoFactory;

        public CSharpDecompiler(IAssemblyResolver assemblyResolver, Func<Stream, IDisposableDebugInfoProvider> debugInfoFactory) {
            _assemblyResolver = assemblyResolver;
            _debugInfoFactory = debugInfoFactory;
        }

        public void Decompile(CompilationStreamPair streams, TextWriter codeWriter, IWorkSession session) {
            Argument.NotNull(nameof(streams), streams);
            Argument.NotNull(nameof(codeWriter), codeWriter);
            Argument.NotNull(nameof(session), session);

            using var assemblyFile = new PEFile("", streams.AssemblyStream);
            using var debugInfo = streams.SymbolStream != null ? _debugInfoFactory(streams.SymbolStream) : null;

            var decompiler = new ICSharpCode.Decompiler.CSharp.CSharpDecompiler(assemblyFile, _assemblyResolver, DecompilerSettings) {
                DebugInfoProvider = debugInfo
            };
            var syntaxTree = decompiler.DecompileWholeModuleAsSingleFile();

            SortTree(syntaxTree);

            new ExtendedCSharpOutputVisitor(codeWriter, FormattingOptions)
                .VisitSyntaxTree(syntaxTree);
        }

        private void SortTree(SyntaxTree root) {
            var firstMovedNode = (AstNode?)null;
            foreach (var node in root.Children) {
                if (node == firstMovedNode)
                    break;

                if (node is NamespaceDeclaration @namespace && IsCompilerGenerated(@namespace)) {
                    node.Remove();
                    root.AddChildWithExistingRole(node);
                    firstMovedNode ??= node;
                }
            }
        }

        private bool IsCompilerGenerated(NamespaceDeclaration @namespace) {
            foreach (var member in @namespace.Members) {
                if (member is not TypeDeclaration type)
                    return false;

                if (!IsCompilerGenerated(type))
                    return false;
            }

            return true;
        }

        private bool IsCompilerGenerated(TypeDeclaration type) {
            foreach (var section in type.Attributes) {
                foreach (var attribute in section.Attributes) {
                    if (attribute.Type is SimpleType { Identifier: nameof(CompilerGeneratedAttribute) or "CompilerGenerated" })
                        return true;
                }
            }
            return false;
        }

        public string LanguageName => TargetNames.CSharp;

        private static CSharpFormattingOptions CreateFormattingOptions()
        {
            var options = FormattingOptionsFactory.CreateAllman();
            options.IndentationString = "    ";
            options.MinimumBlankLinesBetweenTypes = 1;
            return options;
        }

        private class ExtendedCSharpOutputVisitor : CSharpOutputVisitor {
            public ExtendedCSharpOutputVisitor(TextWriter textWriter, CSharpFormattingOptions formattingPolicy) : base(textWriter, formattingPolicy) {
            }

            public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration) {
                base.VisitTypeDeclaration(typeDeclaration);
                if (typeDeclaration.NextSibling is NamespaceDeclaration or TypeDeclaration)
                    NewLine();
            }

            public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration) {
                base.VisitNamespaceDeclaration(namespaceDeclaration);
                if (namespaceDeclaration.NextSibling is NamespaceDeclaration or TypeDeclaration)
                    NewLine();
            }

            public override void VisitAttributeSection(AttributeSection attributeSection) {
                base.VisitAttributeSection(attributeSection);
                if (attributeSection is { AttributeTarget: "assembly" or "module", NextSibling: not AttributeSection { AttributeTarget: "assembly" or "module" } })
                    NewLine();
            }
        }
    }
}