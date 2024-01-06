using Azure.Security.KeyVault.Secrets;
using SharpLab.Server.Common;

namespace SharpLab.Server.Integration.Azure;

public class KeyVaultSecretsClient : ISecretsClient {
    private readonly SecretClient _secretClient;

    public KeyVaultSecretsClient(SecretClient secretClient) {
        _secretClient = secretClient;
    }

    public string GetSecret(string key) {
        return _secretClient.GetSecret(key).Value.Value;
    }
}
