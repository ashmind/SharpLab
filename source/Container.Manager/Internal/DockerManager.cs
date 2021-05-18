using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace SharpLab.Container.Manager.Internal {
    public class DockerManager {
        public async Task<string> ExecuteAsync(Stream assemblyStream, CancellationToken cancellationToken) {
            var client = new DockerClientConfiguration().CreateClient();
            string? containerId;
            try {
                containerId = await CreateContainerAndGetIdAsync(client, cancellationToken);
            }
            catch {
                client.Dispose();
                throw;
            }

            try {
                return await ProcessAssemblyAndDisposeClientAsync(client, containerId, assemblyStream, cancellationToken);
            }
            catch (Exception) {
                //StopAndRemoveContainerAndDisposeClient(client, containerId);
                throw;
            }
        }

        private async Task<string> CreateContainerAndGetIdAsync(DockerClient client, CancellationToken cancellationToken) {
            //var memoryLimit = 10 * 1024 * 1024;
            var created = await client.Containers.CreateContainerAsync(new CreateContainerParameters {
                Image = "mcr.microsoft.com/dotnet/runtime:5.0",
                Cmd = new[] { @"c:\\app\SharpLab.Container.exe" },

                AttachStdout = true,
                //AttachStdin = true,
                //StdinOnce = true,

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
                    //CPUQuota = 50000,

                    AutoRemove = true
                }
            }, cancellationToken);

            return created.ID;
        }

        private async Task<string> ProcessAssemblyAndDisposeClientAsync(DockerClient client, string containerId, Stream assemblyStream, CancellationToken cancellationToken) {
            using var stream = await client.Containers.AttachContainerAsync(containerId, tty: false, new ContainerAttachParameters {
                Stream = true,
                //Stdin = true,
                Stdout = true
            }, cancellationToken);

            //await stream.CopyFromAsync(assemblyStream, cancellationToken);
            var started = await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), cancellationToken);
            if (!started) {
                client.Dispose();
                //StopAndRemoveContainerAndDisposeClient(client, containerId);
                return "Not started?";
            }

            using var waitCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            waitCancellationSource.CancelAfter(300);
            try {
                await client.Containers.WaitContainerAsync(containerId, waitCancellationSource.Token);
            }
            catch (OperationCanceledException) {
                var (outputBeforeCancelled, _) = await stream.ReadOutputToEndAsync(cancellationToken);
                StopAndRemoveContainerAndDisposeClient(client, containerId);
                return outputBeforeCancelled + "\r\nCancelled";
            }

            var (output, _) = await stream.ReadOutputToEndAsync(cancellationToken);
            client.Dispose();
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
