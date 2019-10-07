using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AppDomainToolkit;
using AshMind.Extensions;
using Microsoft.IO;
using MirrorSharp.Advanced;
using Mono.Cecil.Cil;
using Unbreakable;
using SharpLab.Server.Common;
using SharpLab.Server.Execution.Internal;
using SharpLab.Server.Monitoring;
using IAssemblyResolver = Mono.Cecil.IAssemblyResolver;
using SharpLab.Server.AspNetCore.Execution;

namespace SharpLab.Server.Execution {
    public class Executor : ExecutorBase {
        public Executor(
            IAssemblyResolver assemblyResolver,
            ISymbolReaderProvider symbolReaderProvider,
            ApiPolicy apiPolicy,
            IReadOnlyCollection<IAssemblyRewriter> rewriters,
            RecyclableMemoryStreamManager memoryStreamManager,
            ExecutionResultSerializer serializer,
            IMonitor monitor
        ) : base(
            assemblyResolver,
            symbolReaderProvider,
            apiPolicy,
            rewriters,
            memoryStreamManager,
            serializer,
            monitor
        ) {
        }

        protected override ExecutionResultWithException ExecuteWithIsolation(MemoryStream assemblyStream, RuntimeGuardToken guardToken, IWorkSession session) {
            var currentSetup = AppDomain.CurrentDomain.SetupInformation;
            using (var context = AppDomainContext.Create(new AppDomainSetup {
                ApplicationBase = currentSetup.ApplicationBase,
                PrivateBinPath = currentSetup.PrivateBinPath
            })) {
                context.LoadAssembly(LoadMethod.LoadFrom, Assembly.GetExecutingAssembly().GetAssemblyFile().FullName);
                return RemoteFunc.Invoke(context.Domain, assemblyStream.ToArray(), guardToken, Current.ProcessId, ProfilerState.Active, Remote.Execute);
            }
        }

        private static class Remote {
            public static ExecutionResultWithException Execute(byte[] assemblyBytes, RuntimeGuardToken guardToken, int processId, bool profilerActive) {
                var assembly = Assembly.Load(assemblyBytes);
                return IsolatedExecutorCore.Execute(assembly, guardToken.Guid, processId, profilerActive);
            }
        }
    }
}