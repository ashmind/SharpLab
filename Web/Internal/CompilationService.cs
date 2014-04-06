using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;

namespace TryRoslyn.Web.Internal {
    public class CompilationService {
        public string CompileThenDecompile(string code) {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            using (var stream = new MemoryStream()) {
                var emitResult = CSharpCompilation.Create("Test")
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .AddReferences(new MetadataFileReference(typeof(object).Assembly.Location))
                    .AddSyntaxTrees(syntaxTree)
                    .Emit(stream);

                if (!emitResult.Success)
                    throw new CompilationException(string.Join(Environment.NewLine, emitResult.Diagnostics));

                stream.Seek(0, SeekOrigin.Begin);

                return Decompile(stream);
            }
        }

        private static string Decompile(Stream stream) {
            var module = ModuleDefinition.ReadModule(stream);
            var decompiler = new AstBuilder(new DecompilerContext(module) {
                Settings = {
                    AnonymousMethods = false,
                    YieldReturn = false,
                    AsyncAwait = false/*,
                    CSharpFormattingOptions = {
                        BlankLinesBetweenMembers = 2,
                        BlankLinesAfterUsings = 2,
                        BlankLinesBetweenTypes = 2
                    }*/
                }
            });
            decompiler.AddAssembly(module.Assembly);
            decompiler.RunTransformations();
            var resultWriter = new StringWriter();
            decompiler.GenerateCode(new PlainTextOutput(resultWriter));
            return resultWriter.ToString();
        }
    }
}