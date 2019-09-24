using System;
using System.Linq;
using System.Runtime.InteropServices;
using Autofac;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Runtime;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common;
using SharpLab.Server.Execution.Internal;
using SharpLab.Server.Execution.Unbreakable;

namespace SharpLab.Server.Execution {
    [UsedImplicitly]
    public class ExecutionModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterInstance(ApiPolicySetup.CreatePolicy())
                   .AsSelf()
                   .SingleInstance();

            builder.RegisterType<MemoryInspector>()
                   .As<IMemoryInspector>()
                   .SingleInstance();

            builder.Register(_ => {
                var dataTarget = DataTarget.AttachToProcess(Current.ProcessId, uint.MaxValue, AttachFlag.Passive);
                var clrFlavor = RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase)
                    ? ClrFlavor.Core
                    : ClrFlavor.Desktop;
                return dataTarget.ClrVersions.Single(c => c.Flavor == clrFlavor).CreateRuntime();
            }).SingleInstance();

            builder.RegisterType<ExecutionResultSerializer>()
                   .AsSelf()
                   .SingleInstance();

            builder.RegisterType<FlowReportingRewriter>()
                   .As<IAssemblyRewriter>()
                   .SingleInstance();

            builder.RegisterType<MemoryGraphArgumentNamesRewriter>()
                   .As<IAssemblyRewriter>()
                   .SingleInstance();

            builder.RegisterType<FSharpEntryPointRewriter>()
                   .As<IAssemblyRewriter>()
                   .SingleInstance();
        }
    }
}
