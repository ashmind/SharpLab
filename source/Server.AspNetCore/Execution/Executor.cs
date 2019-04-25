using System.Collections.Generic;
using System.IO;
using Microsoft.IO;
using MirrorSharp.Advanced;
using Mono.Cecil.Cil;
using Unbreakable;
using SharpLab.Server.Common;
using SharpLab.Server.Execution.Internal;
using SharpLab.Server.Monitoring;
using IAssemblyResolver = Mono.Cecil.IAssemblyResolver;
using SharpLab.Server.Execution;
using SharpLab.Server.AspNetCore.Platform;
using System.Runtime.Serialization.Formatters.Binary;

namespace SharpLab.Server.AspNetCore.Execution {
    public class Executor : ExecutorBase {
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;

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
            _memoryStreamManager = memoryStreamManager;
        }

        protected override ExecutionResultWithException ExecuteWithIsolation(MemoryStream assemblyStream, RuntimeGuardToken guardToken, IWorkSession session) {
            using (var context = new CustomAssemblyLoadContext(shouldShareAssembly: _ => false)) {
                var assembly = context.LoadFromStream(assemblyStream);
                var serverAssembly = context.LoadFromAssemblyPath(Current.AssemblyPath);

                var coreType = serverAssembly.GetType(typeof(IsolatedExecutorCore).FullName);
                var execute = coreType.GetMethod(nameof(IsolatedExecutorCore.Execute));

                var wrapperInContext = execute.Invoke(null, new object[] { assembly, guardToken.Guid, Current.ProcessId });
                // Since wrapperInContext belongs to a different AssemblyLoadContext, it is not possible to convert
                // it to same type in the default context without some trick (e.g. serialization).
                using (var wrapperStream = _memoryStreamManager.GetStream())
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(wrapperStream, wrapperInContext);
                    wrapperStream.Seek(0, SeekOrigin.Begin);
                    return (ExecutionResultWithException)formatter.Deserialize(wrapperStream);
                }
            }
        }
    }
}