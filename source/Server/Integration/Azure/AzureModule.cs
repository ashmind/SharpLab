using System;
using Autofac;
using Autofac.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Cosmos.Table;
using SharpLab.Server.Caching.Internal;
using SharpLab.Server.Common;
using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Integration.Azure {
    [UsedImplicitly]
    public class AzureModule : Module {
        protected override void Load(ContainerBuilder builder) {
            // This is available even on local (through a mock)
            RegisterCacheStore(builder);

            var keyVaultUrl = Environment.GetEnvironmentVariable("SHARPLAB_KEY_VAULT_URL");
            if (keyVaultUrl == null)
                return;

            RegisterKeyVault(builder, keyVaultUrl);
            RegisterTableStorage(builder);
            RegisterApplicationInsights(builder);
        }

        private void RegisterCacheStore(ContainerBuilder builder) {
            const string cacheClientName = "BlobContainerClient-CacheClient";
            var cachePathPrefix = EnvironmentHelper.GetRequiredEnvironmentVariable("SHARPLAB_CACHE_PATH_PREFIX");

            builder
                .Register(c => {
                    var connectionString = c.Resolve<ISecretsClient>().GetSecret("PublicStorageConnectionString");
                    return new BlobContainerClient(connectionString, "cache");
                })
                .Named<BlobContainerClient>(cacheClientName)
                .SingleInstance();

            builder
                .RegisterType<AzureBlobResultCacheStore>()
                .As<IResultCacheStore>()
                .SingleInstance()
                .WithParameter("cachePathPrefix", cachePathPrefix)
                .WithParameter(new ResolvedParameter(
                    (p, c) => p.ParameterType == typeof(BlobContainerClient),
                    (p, c) => c.ResolveNamed<BlobContainerClient>(cacheClientName)
                ));
        }

        private void RegisterKeyVault(ContainerBuilder builder, string keyVaultUrl) {
            var secretClient = new SecretClient(new Uri(keyVaultUrl), new ManagedIdentityCredential());
            builder.RegisterInstance(secretClient)
                   .AsSelf();

            builder.RegisterType<KeyVaultSecretsClient>()
                   .As<ISecretsClient>()
                   .SingleInstance();
        }

        private void RegisterTableStorage(ContainerBuilder builder) {
            builder.Register(c => {
                var connectionString = c.Resolve<ISecretsClient>().GetSecret("StorageConnectionString");
                return CloudStorageAccount.Parse(connectionString).CreateCloudTableClient();
            }).AsSelf()
              .SingleInstance();

            builder.RegisterType<TableStorageFeatureFlagClient>()
                   .As<IFeatureFlagClient>()
                   .AsSelf()
                   .WithParameter("flagKeys", new[] { "ContainerExperimentRollout" })
                   .WithParameter(new ResolvedParameter(
                       (p, _) => p.ParameterType == typeof(CloudTable),
                       (_, c) => c.Resolve<CloudTableClient>().GetTableReference("featureflags")
                    ))
                   .SingleInstance();

            builder.RegisterBuildCallback(c => c.Resolve<TableStorageFeatureFlagClient>().Start());
        }

        private void RegisterApplicationInsights(ContainerBuilder builder) {
            builder.Register(c => {
                var instrumentationKey = c.Resolve<ISecretsClient>().GetSecret("AppInsightsInstrumentationKey"); 
                var configuration = new TelemetryConfiguration { InstrumentationKey = instrumentationKey };
                return new TelemetryClient(configuration);
            }).AsSelf()
              .SingleInstance();

            var webAppName = EnvironmentHelper.GetRequiredEnvironmentVariable("SHARPLAB_WEBAPP_NAME");
            builder.RegisterType<ApplicationInsightsMonitor>()
                   .As<IMonitor>()
                   .WithParameter("webAppName", webAppName)
                   .SingleInstance();
        }
    }
}