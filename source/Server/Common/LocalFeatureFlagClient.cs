using System;

namespace SharpLab.Server.Common {
    public class LocalFeatureFlagClient : IFeatureFlagClient {
        public int? GetInt32Flag(string key) {
            var environmentKey = $"SHARPLAB_LOCAL_FLAGS_{key}";
            return Environment.GetEnvironmentVariable(environmentKey) is {} value
                 ? int.Parse(value)
                 : null;
        }
    }
}
