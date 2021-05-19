using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
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
        private readonly MemoryCache _memoryCache = new("_");

        private Task? _startTask;

        public DockerManager(
            StdinWriter stdinWriter,
            StdoutReader stdoutReader,
            DockerClientConfiguration clientConfiguration
        ) {
            _stdinWriter = stdinWriter;
            _stdoutReader = stdoutReader;
            _clientConfiguration = clientConfiguration;
        }

        public void Start() {
            _startTask = StartAsync();
        }

        private async Task StartAsync() {
            using var client = _clientConfiguration.CreateClient();

            var removeTasks = new List<Task>();
            foreach (var container in await client.Containers.ListContainersAsync(new ContainersListParameters { All = true })) {
                if (!container.Names.Any(n => n.StartsWith("/sharplab_")))
                    continue;

                removeTasks.Add(StopAndRemoveContainerAsync(client, container.ID));
            }
            await Task.WhenAll(removeTasks);
        }

        public async Task<ReadOnlyMemory<char>> ExecuteAsync(string sessionId, byte[] assemblyBytes, CancellationToken cancellationToken) {
            await (_startTask ?? throw new InvalidOperationException("Start must be called before ExecuteAsync"));

            // Note that _containers are never accessed through multiple threads for the same session id,
            // so atomicity is not required within same session id
            if (_memoryCache.Get(sessionId) is not SessionContainer container) {
                container = await CreateAndStartContainerAsync(sessionId, cancellationToken);
                _memoryCache.Set(sessionId, container, GetContainerCachePolicy());
            }

            try {
                var (output, outputFailed) = await ExecuteInContainerAsync(container, assemblyBytes, cancellationToken);
                if (outputFailed)
                    _memoryCache.Remove(sessionId);
                return output;
            }
            catch {
                _memoryCache.Remove(sessionId);
                throw;
            }
        }

        private async Task<SessionContainer> CreateAndStartContainerAsync(string sessionId, CancellationToken cancellationToken) {
            //var memoryLimit = 10 * 1024 * 1024;
            var client = _clientConfiguration.CreateClient();
            string containerId;
            try {
                containerId = (await client.Containers.CreateContainerAsync(new CreateContainerParameters {
                    Name = $"sharplab_{sessionId}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}",
                    Image = "mcr.microsoft.com/dotnet/runtime:5.0",
                    Cmd = new[] { @"c:\\app\SharpLab.Container.exe" },

                    AttachStdout = true,
                    AttachStdin = true,
                    OpenStdin = true,
                    StdinOnce = true,

                    //NetworkDisabled = true,
                    HostConfig = new HostConfig {
                        Mounts = new[] {
                            new Mount {
                                Source = AppDomain.CurrentDomain.BaseDirectory,
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

            MultiplexedStream? stream = null;
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
                CleanupContainerAndDisposeClient(client, containerId, stream);
                throw;
            }

            return new(sessionId, client, containerId, stream);
        }

        private CacheItemPolicy GetContainerCachePolicy() => new() {
            AbsoluteExpiration = DateTime.Now.AddMinutes(5),
            RemovedCallback = c => {
                var container = (SessionContainer)c.CacheItem.Value;
                CleanupContainerAndDisposeClient(container.Client, container.ContainerId, container.Stream);
            }
        };

        private async Task<(ReadOnlyMemory<char> output, bool outputFailed)> ExecuteInContainerAsync(SessionContainer container, byte[] assemblyBytes, CancellationToken cancellationToken) {
            var outputEndMarker = "---END-OUTPUT-" + Guid.NewGuid().ToString();

            await _stdinWriter.WriteCommandAsync(container.Stream, new ExecuteCommand(assemblyBytes, outputEndMarker), cancellationToken);

            using var executeCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            executeCancellationSource.CancelAfter(1000);
            return await _stdoutReader.ReadOutputAsync(container.Stream, outputEndMarker, executeCancellationSource.Token);
        }

        public void CleanupContainerAndDisposeClient(DockerClient client, string containerId, MultiplexedStream? stream) {
            async Task StopContainerAndDisposeClientAsync() {
                try {
                    stream?.Dispose();
                }
                catch (Exception ex) {
                    Console.Error.WriteLine(ex);
                }

                await StopAndRemoveContainerAsync(client, containerId);

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

        private async Task StopAndRemoveContainerAsync(DockerClient client, string containerId) {
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
        }
    }
}
