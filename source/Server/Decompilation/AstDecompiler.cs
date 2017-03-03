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
using TryRoslyn.Server.Decompilation.Support;

namespace TryRoslyn.Server.Decompilation {
    public abstract class AstDecompiler : IDecompiler {
        private static readonly ConcurrentDictionary<string, AssemblyDefinition> AssemblyCache = new ConcurrentDictionary<string, AssemblyDefinition>();

        public void Decompile(Stream assemblyStream, TextWriter resultWriter) {
            // ReSharper disable once AgentHeisenbug.CallToNonThreadSafeStaticMethodInThreadSafeType
            var module = ModuleDefinition.ReadModule(assemblyStream);
            ((BaseAssemblyResolver)module.AssemblyResolver).ResolveFailure += (_, name) => AssemblyCache.GetOrAdd(name.FullName, fullName => {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.FullName == fullName);
                return AssemblyDefinition.ReadAssembly(assembly.GetAssemblyFile().FullName);
            });

            var context = new DecompilerContext(module) {
                Settings = {
                    AnonymousMethods = false,
                    YieldReturn = false,
                    AsyncAwait = false,
                    AutomaticProperties = false,
                    ExpressionTrees = false
                }
            };

            var ast = new AstBuilder(context);
            ast.AddAssembly(module.Assembly);

            RunTransforms(ast, context);

            // I cannot use GenerateCode as it re-runs all the transforms
            var userCode = GetUserCode(ast);
            WriteResult(resultWriter, userCode, context);
        }

        protected abstract void WriteResult(TextWriter writer,  IEnumerable<AstNode> ast, DecompilerContext context);

        private IEnumerable<AstNode> GetUserCode(AstBuilder ast) {
            //if (!scriptMode)
                return new[] { ast.SyntaxTree };

            //var scriptClass = ast.CompilationUnit.Descendants.OfType<TypeDeclaration>().First(t => t.Name == "Script");
            //return FlattenScript(scriptClass);
        }

        //private IEnumerable<AstNode> FlattenScript(TypeDeclaration scriptClass) {
        //    foreach (var member in scriptClass.Members) {
        //        var constructor = member as ConstructorDeclaration;
        //        if (constructor != null) {
        //            foreach (var statement in constructor.Body.Statements) {
        //                yield return statement;
        //            }
        //        }
        //        else {
        //            yield return member;
        //        }
        //    }
        //}

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