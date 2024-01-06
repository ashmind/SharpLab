using System;
using Autofac;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using SharpLab.Server.Common;
using SharpLab.Server.Integration.Azure;
using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Azure;

[UsedImplicitly]
public class AzureModule : Module {
    protected override void Load(ContainerBuilder builder) {
        var keyVaultUrl = Environment.GetEnvironmentVariable("SHARPLAB_KEY_VAULT_URL");
        if (keyVaultUrl == null)
            return;

        RegisterKeyVault(builder, keyVaultUrl);
        RegisterApplicationInsights(builder);
    }

    private void RegisterKeyVault(ContainerBuilder builder, string keyVaultUrl) {
        var secretClient = new SecretClient(new Uri(keyVaultUrl), new ManagedIdentityCredential());
        builder.RegisterInstance(secretClient)
               .AsSelf();

        builder.RegisterType<KeyVaultSecretsClient>()
               .As<ISecretsClient>()
               .SingleInstance();
    }

    private void RegisterApplicationInsights(ContainerBuilder builder) {
        builder.Register(c => {
            var connectionString = c.Resolve<ISecretsClient>().GetSecret("AppInsightsConnectionString");
            var configuration = new TelemetryConfiguration { ConnectionString = connectionString };
            return new TelemetryClient(configuration);
        }).AsSelf()
          .SingleInstance();

        builder.RegisterType<ApplicationInsightsMetricMonitor>()
               .AsSelf()
               .InstancePerDependency();

        var webAppName = EnvironmentHelper.GetRequiredEnvironmentVariable("SHARPLAB_WEBAPP_NAME");
        builder.RegisterType<ApplicationInsightsMonitor>()
               .As<IMonitor>()
               .WithParameter("webAppName", webAppName)
               .SingleInstance();
    }
}