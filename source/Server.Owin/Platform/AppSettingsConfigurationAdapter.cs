using System.ComponentModel;
using System.Configuration;
using SharpLab.Server.Common;

namespace SharpLab.Server.Owin.Platform {
    public class AppSettingsConfigurationAdapter : IConfigurationAdapter {
        public TValue GetValue<TValue>(string key) {
            var converter = TypeDescriptor.GetConverter(typeof(TValue));
            var valueString = ConfigurationManager.AppSettings[key];
            return (TValue)converter.ConvertFromInvariantString(valueString);
        }
    }
}
