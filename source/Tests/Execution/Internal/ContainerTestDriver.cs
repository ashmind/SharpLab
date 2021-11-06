using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.CodeAnalysis;
using Microsoft.IO;
using Microsoft.Extensions.Logging.Mocks;
using MirrorSharp.Advanced;
using ProtoBuf;
using SharpLab.Container;
using SharpLab.Container.Manager.Internal;
using SharpLab.Container.Protocol.Stdin;
using SharpLab.Server.Common;
using SharpLab.Server.Compilation;
using SharpLab.Server.Execution;
using SharpLab.Server.Execution.Container;
using SharpLab.Server.Execution.Container.Mocks;
using SharpLab.Tests.Internal;
using LanguageNames = SharpLab.Server.Common.LanguageNames;

namespace SharpLab.Tests.Execution.Internal {
    // TODO: Consolidate into standard SlowUpdate
    public class ContainerTestDriver {
        public static async Task<string> CompileAndExecuteAsync(string code, string languageName = LanguageNames.CSharp, OptimizationLevel optimizationLevel = OptimizationLevel.Debug) {
            var session = await PrepareWorkSessionAsync(code, languageName, optimizationLevel);

            var assemblyStream = new MemoryStream();
            var symbolStream = new MemoryStream();
            var diagnostics = new List<Diagnostic>();
            var (compiled, hasSymbols) = await new Compiler(new RecyclableMemoryStreamManager()).TryCompileToStreamAsync(
                assemblyStream,
                symbolStream,
                session,
                diagnostics,
                CancellationToken.None
            );
            if (!compiled)
                throw new Exception("Compilation failed:\n" + string.Join('\n', diagnostics));
            assemblyStream.Position = 0;
            symbolStream.Position = 0;

            var streams = new CompilationStreamPair(assemblyStream, hasSymbols ? symbolStream : null);
            var executor = CreateContainerExecutor();
            return (await executor.ExecuteAsync(streams, session, CancellationToken.None)).Output;
        }

        private static async Task<IWorkSession> PrepareWorkSessionAsync(string code, string languageName, OptimizationLevel optimizationLevel) {            
            var mirrorsharp = await TestDriverFactory.FromCodeAsync(
                code, languageName, TargetNames.Run,
                optimize: optimizationLevel == OptimizationLevel.Release ? Optimize.Release : Optimize.Debug
            );
            // forces mock to exist
            TestEnvironment.Container.Resolve<ContainerClientMock>();
            await mirrorsharp.SendSlowUpdateAsync();

            return mirrorsharp.Session;
        }

        private static IContainerExecutor CreateContainerExecutor() {
            using var containerScope = TestEnvironment.Container.BeginLifetimeScope(builder => {
                builder.RegisterType<TestContainerClient>().As<IContainerClient>();

                // Override as transient
                builder.RegisterType<ContainerExecutor>()
                       .As<IContainerExecutor>()
                       .InstancePerDependency();
            });
            return containerScope.Resolve<IContainerExecutor>();
        }

        private class TestContainerClient : IContainerClient {
            public async Task<ContainerExecutionResult> ExecuteAsync(string sessionId, Stream assemblyStream, bool includePerformance, CancellationToken cancellationToken) {
                var startMarker = Guid.NewGuid();
                var endMarker = Guid.NewGuid();
                var executeCommand = new ExecuteCommand(
                    ((MemoryStream)assemblyStream).ToArray(),
                    startMarker, endMarker,
                    includePerformance
                );

                var stdin = new MemoryStream();
                Serializer.SerializeWithLengthPrefix(stdin, executeCommand, PrefixStyle.Base128);
                stdin.Seek(0, SeekOrigin.Begin);

                var stdout = new MemoryStream();
                var savedConsoleOut = Console.Out;
                Console.SetOut(new StreamWriter(stdout) { AutoFlush = true });
                try {
                    Program.Run(stdin, stdout, () => {});
                }
                finally {
                    Console.SetOut(savedConsoleOut);
                }

                stdout.Seek(0, SeekOrigin.Begin);
                var stdoutReader = new StdoutReader(new LoggerMock<StdoutReader>());
                var outputResult = await stdoutReader.ReadOutputAsync(
                    stdout,
                    new byte[stdout.Length],
                    Encoding.UTF8.GetBytes(startMarker.ToString()),
                    Encoding.UTF8.GetBytes(endMarker.ToString()),
                    cancellationToken
                );

                return new(Encoding.UTF8.GetString(outputResult.Output.Span), outputFailed: false);
            }
        }
    }
}
