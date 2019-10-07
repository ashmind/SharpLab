namespace SharpLab.WebApp.Integration.GitHub {
    public class GitHubClientSettings {
        public GitHubClientSettings(string clientId, string clientSecret) {
            ClientId = Argument.NotNullOrEmpty(nameof(clientId), clientId);
            ClientSecret = Argument.NotNullOrEmpty(nameof(clientSecret), clientSecret);
        }

        public string ClientId { get; }
        public string ClientSecret { get; }
    }
}
