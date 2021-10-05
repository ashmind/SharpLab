namespace SharpLab.Server.Decompilation {
    public class JitAsmSettings {
        public JitAsmSettings(bool shouldDisableMethodSymbolResolver) {
            ShouldDisableMethodSymbolResolver = shouldDisableMethodSymbolResolver;
        }

        public bool ShouldDisableMethodSymbolResolver { get; }
    }
}
