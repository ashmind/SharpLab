using Microsoft.Extensions.Configuration;
using SharpLab.Server.Common;

namespace SharpLab.Server.Owin.Platform {
    public class ConfigurationAdapter : IConfigurationAdapter {
        private readonly IConfiguration _configuration;

        public ConfigurationAdapter(IConfiguration configuration) {
            _configuration = configuration;
        }

        public TValue GetValue<TValue>(string key) => _configuration.GetValue<TValue>(key);
    }
}
