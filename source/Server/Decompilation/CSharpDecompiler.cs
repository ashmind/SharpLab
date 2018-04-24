using System.IO;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using Mono.Cecil;
using SharpLab.Server.Common;

namespace SharpLab.Server.Decompilation {
    public class CSharpDecompiler : IDecompiler {
        private static readonly CSharpFormattingOptions FormattingOptions = CreateFormattingOptions();
        private static readonly DecompilerSettings DecompilerSettings = new DecompilerSettings {
            AnonymousMethods = false,
            AnonymousTypes = false,
            YieldReturn = false,
            AsyncAwait = false,
            AutomaticProperties = false,
            ExpressionTrees = false,
            ArrayInitializers = false,
            ObjectOrCollectionInitializers = false,
            UsingStatement = false,
            LiftNullables = false,
            NullPropagation = false
        };

        private readonly IAssemblyResolver _assemblyResolver;

        public CSharpDecompiler(IAssemblyResolver assemblyResolver) {
            _assemblyResolver = assemblyResolver;
        }

        public void Decompile(Stream assemblyStream, TextWriter codeWriter) {
            // ReSharper disable once AgentHeisenbug.CallToNonThreadSafeStaticMethodInThreadSafeType
            var readerParameters = new ReaderParameters { AssemblyResolver = _assemblyResolver };
            using (var module = ModuleDefinition.ReadModule(assemblyStream, readerParameters)) {
                var decompiler = new ICSharpCode.Decompiler.CSharp.CSharpDecompiler(module, DecompilerSettings);
                var syntaxTree = decompiler.DecompileWholeModuleAsSingleFile();

                new CSharpOutputVisitor(codeWriter, FormattingOptions).VisitSyntaxTree(syntaxTree);
            }
        }

        public string LanguageName => TargetNames.CSharp;

        private static CSharpFormattingOptions CreateFormattingOptions()
        {
            var options = FormattingOptionsFactory.CreateAllman();
            options.IndentationString = "    ";
            return options;
        }
    }
}