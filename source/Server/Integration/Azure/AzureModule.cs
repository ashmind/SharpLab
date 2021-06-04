using System;
using Autofac;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using SharpLab.Server.Common;
using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Azure {
    [UsedImplicitly]
    public class AzureModule : Module {
        protected override void Load(ContainerBuilder builder) {
            RegisterKeyVault(builder);
            RegisterApplicationInsights(builder);
        }

        private void RegisterKeyVault(ContainerBuilder builder) {
            var keyVaultUrl = Environment.GetEnvironmentVariable("SHARPLAB_KEY_VAULT_URL");
            if (keyVaultUrl == null)
                return;

            var secretClient = new SecretClient(new Uri(keyVaultUrl), new ManagedIdentityCredential());
            builder.RegisterInstance(secretClient)
                   .AsSelf();

            builder.RegisterType<KeyVaultSecretsClient>()
                   .As<ISecretsClient>()
                   .SingleInstance();
        }

        private static void RegisterApplicationInsights(ContainerBuilder builder) {
            // TODO: Load from KeyVault
            var instrumentationKey = Environment.GetEnvironmentVariable("SHARPLAB_TELEMETRY_KEY");
            if (instrumentationKey == null)
                return;

            var configuration = new TelemetryConfiguration { InstrumentationKey = instrumentationKey };
            builder.RegisterInstance(new TelemetryClient(configuration))
                   .AsSelf();

            var webAppName = Environment.GetEnvironmentVariable("SHARPLAB_WEBAPP_NAME");
            builder.RegisterType<ApplicationInsightsMonitor>()
                   .As<IMonitor>()
                   .WithParameter("webAppName", webAppName)
                   .SingleInstance();
        }
    }
}