namespace SharpLab.Server.Common {
    public interface IConfigurationAdapter {
        TValue GetValue<TValue>(string key);
    }
}
