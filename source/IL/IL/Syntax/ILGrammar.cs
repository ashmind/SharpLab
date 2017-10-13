using System.Linq;
using Pidgin;

namespace IL.Syntax {
    using AshMind.Extensions;
    using static Parser;

    internal static class ILGrammar {
        public static Parser<char, CompilationUnitNode> Root { get; } = BuildParser();

        private static Parser<char, Maybe<Unit>> _maybeLineComment;

        private static Parser<char, CompilationUnitNode> BuildParser() {
            _maybeLineComment = SkipWhitespaces
                .Then(String("//"))
                .Then(Parser<char>.Token(c => c != '\r' && c != '\n').SkipMany())
                .Then(SkipWhitespaces)
                .Optional();

            var idToken = LetterOrDigit.AtLeastOnceString();
            var sqStringToken = AnyCharExcept('\'')
                .ManyString()
                .Between(Char('\''));
            var classToken = String(".class");
            var publicToken = ModifierToken(DeclarationModifier.Public);
            var privateToken = ModifierToken(DeclarationModifier.Private);
            var autoToken = ModifierToken(DeclarationModifier.Auto);
            var ansiToken = ModifierToken(DeclarationModifier.Ansi);
            var beforeFieldInitToken = ModifierToken(DeclarationModifier.BeforeFieldInit);
            var ctorToken = String(".ctor");
            var valueTypeToken = String("valuetype");
            var extendsToken = String("extends");
            var implementsToken = String("implements");

            var id = OneOf(idToken, sqStringToken);
            var dottedName = id.SeparatedAtLeastOnce(Char('.'))
                .Select(n => new MultipartIdentifier(n.ToList())); // TODO
            var slashedName = dottedName; // TODO
            var className = SkipWhitespaces.Then(OneOf(
                Map(
                    (assemblyName, name) => new TypeReferenceNode(name, assemblyName),
                    dottedName.Between(Punctuation('['), Punctuation(']')),
                    slashedName
                ),
                slashedName.Select(name => new TypeReferenceNode(name))
            ).Labelled("type name")); // TODO

            var @class = classToken;
            var @public = publicToken;

            var classAttr = OneOf(
                Try(publicToken),
                privateToken,
                Try(autoToken),
                ansiToken,
                beforeFieldInitToken
            ).SeparatedAndTerminated(SkipWhitespaces); // TODO
            var typarAttrib = OneOf(
                String("+"),
                String("-"),
                classToken,
                valueTypeToken,
                ctorToken
            );
            var typarAttribs = typarAttrib.Many();
            var typeSpec = className; // TODO
            var typeList = typeSpec.Separated(Char(','));
            var tyBound = typeList.Between(Char('('), Char(')'));
            var typars = Map(
                (x1, x2, x3) => "TODO", typarAttribs, tyBound.Optional(), dottedName
            ).Many(); // TODO
            var typarsClause = typars.Between(Char('<'), Char('>')).Optional();

            var implList = typeSpec.Separated(Char(','));

            var extendsClause = extendsToken
                .Then(SkipWhitespaces)
                .Then(typeSpec);

            var implementsClause = implementsToken
                .Then(SkipWhitespaces)
                .Then(implList);
            
            var classHead = Map(
                (modifiers, name, x1, extends, x3) => (modifiers, name, extends),
                @class.Then(SkipWhitespaces.Then(classAttr)),
                dottedName.Between(SkipWhitespaces).Labelled("class name"),
                typarsClause,
                extendsClause.Between(SkipWhitespaces).Optional(),
                implementsClause.Between(SkipWhitespaces).Optional()
            );

            Parser<char, DeclarationNode> classDecl = null;
            var classDecls = Rec(() => classDecl).Many();
            classDecl = Map(
                (head, x2) => (DeclarationNode)new ClassDeclarationNode(
                    head.name,
                    head.modifiers.ToSet(),
                    head.extends.HasValue ? head.extends.Value : null
                ),
                classHead, classDecls.Between(Punctuation('{'), Punctuation('}')).Labelled("class body")
            ).Labelled("class declaration");

            var decl = OneOf(classDecl); // TODO
            var decls = decl.Many();

            return Map(
                ds => new CompilationUnitNode(ds.ToList()),
                decls.Before(Parser<char>.End())
            );
        }

        private static Parser<char, char> Punctuation(char value) {
            return Char(value).Between(SkipWhitespaces).Before(_maybeLineComment);
        }

        private static Parser<char, DeclarationModifier> ModifierToken(DeclarationModifier modifier) {
            return String(modifier.Text).Select(_ => modifier);
        }
    }
}
