using Autofac;
using JetBrains.Annotations;
using SharpLab.Server.Caching.Internal;
using SharpLab.Server.Common;

namespace SharpLab.Server.Caching {
    [UsedImplicitly]
    public class CachingModule : Module {
        protected override void Load(ContainerBuilder builder) {
            var webAppName = EnvironmentHelper.GetRequiredEnvironmentVariable("SHARPLAB_WEBAPP_NAME");
            var branchId = webAppName.StartsWith("sl-") ? webAppName : null;

            builder.RegisterType<ResultCacheBuilder>()
                   .As<IResultCacheBuilder>()
                   .WithParameter("branchId", branchId)
                   .SingleInstance();

            builder.RegisterType<ResultCacher>()
                   .As<IResultCacher>()
                   .SingleInstance();
        }
    }
}