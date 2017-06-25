using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AshMind.Extensions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Decompilation {
    public abstract class AstBasedDecompiler : IDecompiler {
        private static readonly ConcurrentDictionary<string, AssemblyDefinition> AssemblyCache = new ConcurrentDictionary<string, AssemblyDefinition>();

        public void Decompile(Stream assemblyStream, TextWriter codeWriter) {
            // ReSharper disable once AgentHeisenbug.CallToNonThreadSafeStaticMethodInThreadSafeType
            var module = ModuleDefinition.ReadModule(assemblyStream);
            ((BaseAssemblyResolver)module.AssemblyResolver).ResolveFailure += (_, name) => AssemblyCache.GetOrAdd(name.FullName, fullName => {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.FullName == fullName);
                return AssemblyDefinition.ReadAssembly(assembly.GetAssemblyFile().FullName);
            });

            var context = new DecompilerContext(module) {
                Settings = {
                    OperatorOverloading = false,
                    AnonymousMethods = false,
                    YieldReturn = false,
                    AsyncAwait = false,
                    AutomaticProperties = false,
                    ExpressionTrees = false,
                    ArrayInitializers = false,
                    ObjectOrCollectionInitializers = false
                }
            };

            var ast = new AstBuilder(context);
            ast.AddAssembly(module.Assembly);

            RunTransforms(ast, context);

            // I cannot use GenerateCode as it re-runs all the transforms
            WriteResult(codeWriter, ast.SyntaxTree, context);
        }

        protected abstract void WriteResult(TextWriter writer, AstNode ast, DecompilerContext context);

        private void RunTransforms(AstBuilder ast, DecompilerContext context) {
            var transforms = TransformationPipeline.CreatePipeline(context).ToList();
            transforms[transforms.FindIndex(t => t is ConvertConstructorCallIntoInitializer)] = new RoslynFriendlyConvertConstructorCallIntoInitializer();

            foreach (var transform in transforms) {
                transform.Run(ast.SyntaxTree);
            }
        }

        public abstract string LanguageName { get; }
    }
}