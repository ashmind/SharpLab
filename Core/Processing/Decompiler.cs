using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.ILSpy.VB;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Visitors;
using JetBrains.Annotations;
using Mono.Cecil;
using TryRoslyn.Core.Processing.DecompilationSupport;
using AstNode = ICSharpCode.NRefactory.CSharp.AstNode;
using TextWriterOutputFormatter = ICSharpCode.NRefactory.CSharp.TextWriterOutputFormatter;

namespace TryRoslyn.Core.Processing {
    [ThreadSafe]
    public class Decompiler : IDecompiler {
        public void Decompile(Stream assemblyStream, TextWriter resultWriter, LanguageIdentifier language) {
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

            // somewhat primitive but not worth classes yet
            if (language == LanguageIdentifier.CSharp) {
                ProcessCSharp(userCode, resultWriter, context);
            }
            else if (language == LanguageIdentifier.VBNet) {
                ProcessVBNet(userCode, resultWriter, context);
            }
            else {
                throw new ArgumentOutOfRangeException("language");
            }
        }

        private static void ProcessCSharp(IEnumerable<AstNode> ast, TextWriter writer, DecompilerContext context) {
            var visitor = new DecompiledPseudoCSharpOutputVisitor(
                new TextWriterOutputFormatter(writer) {
                    IndentationString = "    "
                },
                context.Settings.CSharpFormattingOptions
            );

            foreach (var node in ast) {
                node.AcceptVisitor(visitor);
            }
        }

        private static void ProcessVBNet(IEnumerable<AstNode> ast, TextWriter writer, DecompilerContext context) {
            var converter = new CSharpToVBConverterVisitor(new ILSpyEnvironmentProvider());
            var visitor = new OutputVisitor(
                new VBTextOutputFormatter(new CustomizableIndentPlainTextOutput(writer) {
                    IndentationString = "    "
                }),
                new VBFormattingOptions()
            );
            foreach (var node in ast) {
                node.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
                var converted = node.AcceptVisitor(converter, null);
                converted.AcceptVisitor(visitor, null);
            }
        }

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
    }
}