using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using TryRoslyn.Core.Decompilation.Support;

namespace TryRoslyn.Core.Decompilation {
    public class Decompiler : IDecompiler {
        public void Decompile(Stream assemblyStream, TextWriter resultWriter) {
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

            this.RunTransforms(ast, context);

            // I cannot use GenerateCode as it re-runs all the transforms
            ast.CompilationUnit.AcceptVisitor(new DecompiledPseudoCSharpOutputVisitor(
                new TextWriterOutputFormatter(resultWriter) {
                    IndentationString = "    "
                },
                context.Settings.CSharpFormattingOptions
            ));
        }

        public void RunTransforms(AstBuilder ast, DecompilerContext context) {
            var transforms = TransformationPipeline.CreatePipeline(context).ToList();
            transforms[transforms.FindIndex(t => t is ConvertConstructorCallIntoInitializer)] = new RoslynFriendlyConvertConstructorCallIntoInitializer();

            foreach (var transform in transforms) {
                transform.Run(ast.CompilationUnit);
            }
        }
    }
}