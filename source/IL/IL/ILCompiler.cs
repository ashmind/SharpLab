using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IL.Syntax;
using Mono.Cecil;

namespace IL {
    public class ILCompiler {
        private static readonly IReadOnlyDictionary<DeclarationModifier, TypeAttributes> TypeAttributeMap = new Dictionary<DeclarationModifier, TypeAttributes> {
            { DeclarationModifier.Public, TypeAttributes.Public },
            { DeclarationModifier.Private, TypeAttributes.NotPublic },
            { DeclarationModifier.Auto, TypeAttributes.AutoLayout },
            { DeclarationModifier.Ansi, TypeAttributes.AnsiClass },
            { DeclarationModifier.BeforeFieldInit, TypeAttributes.BeforeFieldInit }
        };

        public void Compile(CompilationUnitNode compilationUnit, Stream stream) {
            var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("_", new Version(0, 0)), "_", ModuleKind.Dll);
            foreach (var declaration in compilationUnit.Declarations) {
                CompileDeclaration(declaration, assembly.MainModule);
            }
            assembly.Write(stream);
        }

        private void CompileDeclaration(DeclarationNode declaration, ModuleDefinition module) {
            switch (declaration) {
                case ClassDeclarationNode c: CompileClassDeclaration(c, module); break;
            }
        }

        private void CompileClassDeclaration(ClassDeclarationNode declaration, ModuleDefinition module) {
            var attributes = TypeAttributes.Class;
            foreach (var modifier in declaration.Modifiers) {
                attributes |= TypeAttributeMap[modifier];
            }
            var type = new TypeDefinition("", declaration.Name, attributes);
            module.Types.Add(type);
        }
    }
}
