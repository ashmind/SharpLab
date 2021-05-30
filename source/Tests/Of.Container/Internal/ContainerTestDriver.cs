using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using MirrorSharp.Advanced.Mocks;
using ProtoBuf;
using SharpLab.Container;
using SharpLab.Container.Protocol.Stdin;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Internal;
using SharpLab.Server.Execution;
using SharpLab.Server.Execution.Container;
using SharpLab.Tests.Internal;

namespace SharpLab.Tests.Of.Container.Internal {
    public class ContainerTestDriver {
        public static async Task<string> CompileAndExecuteAsync(string code) {
            var project = GetProject(code);

            var session = new WorkSessionMock();
            session.Setup.LanguageName.Returns(LanguageNames.CSharp);
            session.Setup.ExtensionData.Returns(new Dictionary<string, object>());
            session.Setup.IsRoslyn.Returns(true);

            var roslynSessionMock = new RoslynSessionMock();
            roslynSessionMock.Setup.Project.Returns(project);
            session.Setup.Roslyn.Returns(roslynSessionMock);

            session.SetContainerExperimentAllowed(true);

            var executor = CreateContainerExecutor();
            return await executor.ExecuteAsync(await CompileAsync(project), session, CancellationToken.None);
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

        private static async Task<CompilationStreamPair> CompileAsync(Project project) {
            var assemblyStream = new MemoryStream();
            var symbolStream = new MemoryStream();

            var compilation = (await project.GetCompilationAsync())!;
            var emitResult = compilation.Emit(assemblyStream, pdbStream: symbolStream, options: new(
                debugInformationFormat: DebugInformationFormat.PortablePdb
            ));
            if (!emitResult.Success)
                throw new Exception("Compilation failed:\n" + string.Join('\n', emitResult.Diagnostics));

            assemblyStream.Seek(0, SeekOrigin.Begin);
            symbolStream.Seek(0, SeekOrigin.Begin);

            return new CompilationStreamPair(assemblyStream, symbolStream);
        }

        private class TestContainerClient : IContainerClient {
            public Task<string> ExecuteAsync(string sessionId, Stream assemblyStream, bool includePerformance, CancellationToken cancellationToken) {
                var executeCommand = new ExecuteCommand(((MemoryStream)assemblyStream).ToArray(), Guid.NewGuid(), includePerformance);

                var stdin = new MemoryStream();
                Serializer.SerializeWithLengthPrefix(stdin, executeCommand, PrefixStyle.Base128);
                stdin.Seek(0, SeekOrigin.Begin);

                var stdout = new MemoryStream();
                Program.Run(stdin, stdout);

                return Task.FromResult(Encoding.UTF8.GetString(stdout.ToArray()));
            }
        }
    }
}
