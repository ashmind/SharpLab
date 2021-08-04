using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Docker.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.Advanced.Mocks;
using MirrorSharp.Testing;
using ProtoBuf;
using SharpLab.Container;
using SharpLab.Container.Manager.Internal;
using SharpLab.Container.Protocol.Stdin;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Internal;
using SharpLab.Server.Compilation;
using SharpLab.Server.Execution;
using SharpLab.Server.Execution.Container;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Of.Container.Internal {
    public class ContainerTestDriver {
        public static async Task<string> CompileAndExecuteAsync(string code, string languageName = LanguageNames.CSharp) {
            var session = await PrepareWorkSessionAsync(code, languageName);

            var assemblyStream = new MemoryStream();
            var symbolStream = new MemoryStream();
            var diagnostics = new List<Diagnostic>();
            var (compiled, hasSymbols) = await new Compiler().TryCompileToStreamAsync(
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
            return await executor.ExecuteAsync(streams, session, CancellationToken.None);
        }

        private static async Task<IWorkSession> PrepareWorkSessionAsync(string code, string languageName = LanguageNames.CSharp) {
            if (languageName == LanguageNames.FSharp) {
                var mirrorsharp = MirrorSharpTestDriver.New(
                    options: new MirrorSharpOptions().DisableCSharp().EnableFSharp(o => {
                        o.AssemblyReferencePaths = o.AssemblyReferencePaths.Add(typeof(Inspect).Assembly.Location);
                    }),
                    languageName: LanguageNames.FSharp
                ).SetText(code);
                await mirrorsharp.SendSlowUpdateAsync();
                return mirrorsharp.Session;
            }

            var session = new WorkSessionMock();
            session.Setup.LanguageName.Returns(languageName);
            session.Setup.ExtensionData.Returns(new Dictionary<string, object>());

            var project = GetProject(code);
            session.Setup.IsRoslyn.Returns(true);
            var roslynSessionMock = new RoslynSessionMock();
            roslynSessionMock.Setup.Project.Returns(project);
            session.Setup.Roslyn.Returns(roslynSessionMock);

            return session;
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

        private static Project GetProject(string code) {
            var references = new AssemblyReferenceCollector().SlowGetAllReferencedAssembliesRecursive(
                typeof(object).Assembly,
                typeof(SharpLabObjectExtensions).Assembly
            ).Select(a => MetadataReference.CreateFromFile(a.Location));

            
            var project = new AdhocWorkspace()
                .AddProject("_", LanguageNames.CSharp)
                .AddMetadataReferences(references)
                .AddDocument("_", SourceText.From(code, Encoding.UTF8))
                .Project;
            project = project.WithCompilationOptions(
                project.CompilationOptions!.WithOptimizationLevel(OptimizationLevel.Debug)
            );

            return project;
        }

        private class TestContainerClient : IContainerClient {
            public async Task<string> ExecuteAsync(string sessionId, Stream assemblyStream, bool includePerformance, CancellationToken cancellationToken) {
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
                    Program.Run(stdin, stdout);
                }
                finally {
                    Console.SetOut(savedConsoleOut);
                }

                stdout.Seek(0, SeekOrigin.Begin);
                var stdoutReader = new StdoutReader();
                var outputResult = await stdoutReader.ReadOutputAsync(
                    new MultiplexedStream(stdout, multiplexed: false),
                    new byte[stdout.Length],
                    Encoding.UTF8.GetBytes(startMarker.ToString()),
                    Encoding.UTF8.GetBytes(endMarker.ToString()),
                    cancellationToken
                );

                return Encoding.UTF8.GetString(outputResult.Output.Span);
            }
        }
    }
}
