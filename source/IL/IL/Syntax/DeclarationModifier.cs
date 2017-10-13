namespace IL.Syntax {
    public class DeclarationModifier {
        private DeclarationModifier(string text) {
            Text = text;
        }

        public string Text { get; }

        public static DeclarationModifier Private { get; } = new DeclarationModifier("private");
        public static DeclarationModifier Public { get; } = new DeclarationModifier("public");
        public static DeclarationModifier Auto { get; } = new DeclarationModifier("auto");
        public static DeclarationModifier Ansi { get; } = new DeclarationModifier("ansi");
        public static DeclarationModifier BeforeFieldInit { get; } = new DeclarationModifier("beforefieldinit");
    }
}
