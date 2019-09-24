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
using SharpLab.Server.Execution;
using SharpLab.Server.AspNetCore.Execution;
using SharpLab.Runtime.Internal;

namespace SharpLab.Server.Owin.Execution {
    public class Executor : ExecutorBase {
        public Executor(
            IAssemblyResolver assemblyResolver,
            ISymbolReaderProvider symbolReaderProvider,
            ApiPolicy apiPolicy,
            IReadOnlyCollection<IAssemblyRewriter> rewriters,
            RecyclableMemoryStreamManager memoryStreamManager,
            IMemoryInspector heapInspector,
            ExecutionResultSerializer serializer,
            IMonitor monitor
        ) : base(
            assemblyResolver,
            symbolReaderProvider,
            apiPolicy,
            rewriters,
            memoryStreamManager,
            heapInspector,
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
                var otherArguments = new SerializableArguments(guardToken, Current.ProcessId, ProfilerState.Active);
                return RemoteFunc.Invoke(context.Domain, assemblyStream.ToArray(), HeapInspector, otherArguments, Remote.Execute);
            }
        }

        private static class Remote {
            public static ExecutionResultWithException Execute(byte[] assemblyBytes, IMemoryInspector heapInspector, SerializableArguments arguments) {
                var assembly = Assembly.Load(assemblyBytes);
                return IsolatedExecutorCore.Execute(assembly, arguments.GuardToken.Guid, heapInspector, arguments.ProcessId, arguments.ProfilerActive);
            }
        }

        [Serializable]
        private struct SerializableArguments {
            public SerializableArguments(RuntimeGuardToken guardToken, int processId, bool profilerActive) : this() {
                GuardToken = guardToken;
                ProcessId = processId;
                ProfilerActive = profilerActive;
            }

            public RuntimeGuardToken GuardToken { get; set; }
            public int ProcessId { get; set; }
            public bool ProfilerActive { get; set; }
        }
    }
}