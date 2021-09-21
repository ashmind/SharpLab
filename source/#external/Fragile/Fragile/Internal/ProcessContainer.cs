using System;
using System.Diagnostics;
using System.IO;
using Fs.Processes.JobObjects;
using Vanara.PInvoke;

namespace Fragile.Internal {
    internal class ProcessContainer : IProcessContainer {
        private readonly StandardStreams _streams;
        private readonly JobObject _jobObject;
        private readonly string _appContainerProfileName;

        public ProcessContainer(
            Process process,
            StandardStreams streams,
            JobObject jobObject,
            string appContainerProfileName
        ) {
            Argument.NotNull(nameof(process), process);
            Argument.NotNull(nameof(jobObject), jobObject);
            Argument.NotNullOrEmpty(nameof(appContainerProfileName), appContainerProfileName);

            Process = process;
            _streams = streams;
            _jobObject = jobObject;
            _appContainerProfileName = appContainerProfileName;
        }

        public Process Process { get; }
        public Stream InputStream => _streams.Input;
        public Stream OutputStream => _streams.Output;
        public Stream ErrorStream => _streams.Error;

        internal static void Dispose(
            Process? process,
            StandardStreams? streams,
            JobObject? jobObject,
            string? appContainerProfileName
        ) {
            SafeDispose.Dispose(
                process, static p => {
                    if (p == null)
                        return;
                    p.Kill();
                    if (!p.WaitForExit(10 * 1000))
                        throw new Exception("Process failed to exit within 10 seconds.");
                },
                process, static p => p?.Dispose(),
                jobObject, static j => j?.Dispose(),
                streams, static s => s?.Dispose(),
                appContainerProfileName, static n => {
                    if (n == null)
                        return;
                    UserEnv.DeleteAppContainerProfile(n).ThrowIfFailed();
                }
            );
        }

        public void Dispose() {
            Dispose(Process, _streams, _jobObject, _appContainerProfileName);
        }
    }
}
