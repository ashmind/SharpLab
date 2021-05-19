using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using SharpLab.Container.Protocol.Stdin;

namespace SharpLab.Container.Manager.Internal {
    public class DockerManager {
        private readonly StdinWriter _stdinWriter;
        private readonly StdoutReader _stdoutReader;
        private readonly DockerClientConfiguration _clientConfiguration;
        private readonly ConcurrentDictionary<string, SessionContainer> _containers = new ConcurrentDictionary<string, SessionContainer>();

        public DockerManager(StdinWriter stdinWriter, StdoutReader stdoutReader, DockerClientConfiguration clientConfiguration) {
            _stdinWriter = stdinWriter;
            _stdoutReader = stdoutReader;
            _clientConfiguration = clientConfiguration;
        }

        public async Task<string> ExecuteAsync(string sessionId, byte[] assemblyBytes, CancellationToken cancellationToken) {
            // Note that _containers are never accessed through multiple threads for the same session id,
            // so atomicity is not required within same session id
            if (!_containers.TryGetValue(sessionId, out var container)) {
                container = await CreateAndStartContainerAsync(sessionId, cancellationToken);
                if (!_containers.TryAdd(sessionId, container))
                    throw new Exception($"Unexpected concurrency conflict within same sessionId {sessionId}: TryAdd failed.");
            }

            try {
                return await ExecuteInContainerAsync(container, assemblyBytes, cancellationToken);
            }
            catch {
                //StopAndRemoveContainerAndDisposeClient(client, containerId);
                throw;
            }
        }

        private async Task<SessionContainer> CreateAndStartContainerAsync(string sessionId, CancellationToken cancellationToken) {
            //var memoryLimit = 10 * 1024 * 1024;
            var client = _clientConfiguration.CreateClient();
            string containerId;
            try {
                containerId = (await client.Containers.CreateContainerAsync(new CreateContainerParameters {
                    Name = $"sharplab--{sessionId}--{DateTime.Now:yyyy-MM-dd--HH-mm-ss}",
                    Image = "mcr.microsoft.com/dotnet/runtime:5.0",
                    Cmd = new[] { @"c:\\app\SharpLab.Container.exe" },

                    AttachStdout = true,
                    AttachStdin = true,
                    OpenStdin = true,

                    //NetworkDisabled = true,
                    HostConfig = new HostConfig {
                        Mounts = new[] {
                            new Mount {
                                Source = @"d:\Development\VS 2019\SharpLab\source\Container\bin\Debug\net5.0",
                                Target = @"c:\app",
                                Type = "bind",
                                ReadOnly = true
                            }
                        },
                        //Memory = memoryLimit,
                        //MemorySwap = memoryLimit,
                        CPUQuota = 50000,

                        AutoRemove = true
                    }
                }, cancellationToken)).ID;
            }
            catch {
                client.Dispose();
                throw;
            }

            MultiplexedStream stream;
            try {
                stream = await client.Containers.AttachContainerAsync(containerId, tty: false, new ContainerAttachParameters {
                    Stream = true,
                    Stdin = true,
                    Stdout = true
                }, cancellationToken);

                try {
                    await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), cancellationToken);
                }
                catch {
                    stream.Dispose();
                    throw;
                }
            }
            catch {
                StopAndRemoveContainerAndDisposeClient(client, containerId);
                throw;
            }

            return new(sessionId, client, containerId, stream);
        }

        private async Task<string> ExecuteInContainerAsync(SessionContainer container, byte[] assemblyBytes, CancellationToken cancellationToken) {
            var outputEndMarker = "---END-OUTPUT-" + Guid.NewGuid().ToString();
            await _stdinWriter.WriteCommandAsync(container.Stream, new ExecuteCommand(assemblyBytes, outputEndMarker), cancellationToken);

            using var waitCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            //waitCancellationSource.CancelAfter(300);

            var output = await _stdoutReader.ReadOutputAsync(container.Stream, outputEndMarker, waitCancellationSource.Token);
            return "Success?\r\n" + output;
        }

        public void StopAndRemoveContainerAndDisposeClient(DockerClient client, string containerId) {
            async Task StopContainerAndDisposeClientAsync() {
                try {
                    await client.Containers.StopContainerAsync(
                        containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 1 }
                    );
                }
                catch (Exception ex) {
                    Console.Error.WriteLine(ex);
                }

                try {
                    await client.Containers.RemoveContainerAsync(
                        containerId, new ContainerRemoveParameters { Force = true }
                    );
                }
                catch (Exception ex) {
                    Console.Error.WriteLine(ex);
                }

                try {
                    client.Dispose();
                }
                catch (Exception ex) {
                    Console.Error.WriteLine(ex);
                }
            }

            // explicitly not awaiting, since it can overrun the request time
            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            StopContainerAndDisposeClientAsync();
            #pragma warning restore CS4014
        }
    }
}
