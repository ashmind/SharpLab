namespace SharpLab.Server.Common;

public interface ISecretsClient {
    string GetSecret(string key);
}