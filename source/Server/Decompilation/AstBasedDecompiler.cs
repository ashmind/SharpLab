using System.IO;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using Mono.Cecil;

namespace SharpLab.Server.Decompilation {
    public abstract class AstBasedDecompiler : IDecompiler {
        private readonly IAssemblyResolver _assemblyResolver;

        protected AstBasedDecompiler(IAssemblyResolver assemblyResolver) {
            _assemblyResolver = assemblyResolver;
        }

        public void Decompile(Stream assemblyStream, TextWriter codeWriter) {
            // ReSharper disable once AgentHeisenbug.CallToNonThreadSafeStaticMethodInThreadSafeType
            var module = ModuleDefinition.ReadModule(assemblyStream, new ReaderParameters {
                AssemblyResolver = _assemblyResolver
            });

            var context = new DecompilerContext(module) {
                Settings = {
                    CanInlineVariables = false,
                    OperatorOverloading = false,
                    AnonymousMethods = false,
                    AnonymousTypes = false,
                    YieldReturn = false,
                    AsyncAwait = false,
                    AutomaticProperties = false,
                    ExpressionTrees = false,
                    ArrayInitializers = false,
                    ObjectOrCollectionInitializers = false,
                    LiftedOperators = false,
                    UsingStatement = false
                }
            };

            var ast = new AstBuilder(context);
            ast.AddAssembly(module.Assembly);

            WriteResult(codeWriter, ast);
        }

        protected abstract void WriteResult(TextWriter writer, AstBuilder ast);

        public abstract string LanguageName { get; }
    }
}