using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private readonly IReadOnlyDictionary<string, AssemblyDefinition> _assemblies;

        public ILCompiler() {
            _assemblies = new AssemblyDefinition[] {
                AssemblyDefinition.ReadAssembly(typeof(object).Assembly.Location)
            }.ToDictionary(a => a.Name.Name);
        }

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
            var type = new TypeDefinition("", string.Join(".", declaration.Name.Parts), attributes);
            if (declaration.ExtendsType != null) {
                var typeReference = ToTypeReference(declaration.ExtendsType, module);
                type.BaseType = typeReference;
            }

            module.Types.Add(type);
        }

        private TypeReference ToTypeReference(TypeReferenceNode extendsType, ModuleDefinition module) {
            var assembly = _assemblies[extendsType.AssemblyName.ToUnescapedString()];
            return module.ImportReference(new TypeReference(
                string.Join(".", extendsType.Name.Parts.Take(extendsType.Name.Parts.Count - 1)),
                extendsType.Name.Parts.Last(),
                assembly.MainModule,
                assembly.MainModule
            ));
        }
    }
}
