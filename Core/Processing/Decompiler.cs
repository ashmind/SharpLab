using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.CSharp;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.CSharp;
using TryRoslyn.Core.Processing.DecompilationSupport;

namespace TryRoslyn.Core.Processing {
    [ThreadSafe]
    public class Decompiler : IDecompiler {
        public void Decompile(Stream assemblyStream, TextWriter resultWriter) {
            // ReSharper disable once AgentHeisenbug.CallToNonThreadSafeStaticMethodInThreadSafeType
            var module = ModuleDefinition.ReadModule(assemblyStream);
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
            var visitor = new DecompiledPseudoCSharpOutputVisitor(
                new TextWriterOutputFormatter(resultWriter) {
                    IndentationString = "    "
                },
                context.Settings.CSharpFormattingOptions
            );
            foreach (var node in userCode) {
                node.AcceptVisitor(visitor);
            }
        }

        private IEnumerable<AstNode> GetUserCode(AstBuilder ast) {
            //if (!scriptMode)
                return new[] { ast.CompilationUnit };

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
                transform.Run(ast.CompilationUnit);
            }
        }
    }
}