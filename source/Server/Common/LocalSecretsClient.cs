using System;

namespace SharpLab.Server.Common {
    public class LocalSecretsClient : ISecretsClient {
        public string GetSecret(string key) {
            Argument.NotNullOrEmpty(nameof(key), key);

            var environmentKey = $"SHARPLAB_LOCAL_SECRETS_{key}";
            return Environment.GetEnvironmentVariable(environmentKey)
                ?? throw new Exception($"Secret {environmentKey} was not found");
        }
    }
}
