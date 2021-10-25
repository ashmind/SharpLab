namespace SharpLab.Server.Caching {
    public readonly struct ResultCacheKeyData {
        public ResultCacheKeyData(
            string languageName,
            string targetName,
            string optimize,
            string code
        ) {
            Argument.NotNullOrEmpty(nameof(languageName), languageName);
            Argument.NotNullOrEmpty(nameof(targetName), targetName);
            Argument.NotNull(nameof(code), code);

            LanguageName = languageName;
            TargetName = targetName;
            Optimize = optimize;
            Code = code;
        }

        public string LanguageName { get; }
        public string TargetName { get; }
        public string Optimize { get; }
        public string Code { get; }
    }
}