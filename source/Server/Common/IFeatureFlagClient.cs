namespace SharpLab.Server.Common {
    public interface IFeatureFlagClient {
        int? GetInt32Flag(string key);
    }
}
