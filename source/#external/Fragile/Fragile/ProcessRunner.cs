using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Fragile.Internal;
using Fragile.Internal.WindowObjectAccessControl;
using Fs.Processes.JobObjects;
using Vanara.PInvoke;

namespace Fragile {
    using SafeAllocatedSID = AdvApi32.SafeAllocatedSID;

    [SupportedOSPlatform("windows")]
    public class ProcessRunner : IProcessRunner {
        private static readonly string[] EnvironmentBlock = new[] {
            $"LOCALAPPDATA={Environment.GetEnvironmentVariable("LOCALAPPDATA")}"
        };

        private readonly ProcessRunnerConfiguration _configuration;
        private readonly SecurityIdentifier _essentialAccessCapabilityIdentifier;
        private readonly byte[] _essentialAccessCapabilitySidBytes;
        private readonly string _exeFilePath;
        private readonly StringBuilder _commandLine;

        private bool _initialSetupCompleted;

        public ProcessRunner(ProcessRunnerConfiguration configuration) {
            _configuration = configuration;

            _essentialAccessCapabilityIdentifier = new SecurityIdentifier(_configuration.EssentialAccessCapabilitySid);
            _essentialAccessCapabilitySidBytes = new byte[_essentialAccessCapabilityIdentifier.BinaryLength];
            _essentialAccessCapabilityIdentifier.GetBinaryForm(_essentialAccessCapabilitySidBytes, 0);

            _exeFilePath = Path.Combine(_configuration.WorkingDirectoryPath, _configuration.ExeFileName);
            _commandLine = new StringBuilder(_exeFilePath.Length + 2)
                .Append("\"")
                .Append(_exeFilePath)
                .Append("\""); // args not supported yet
        }

        public void InitialSetup() {
            /*var workingDirectory = new DirectoryInfo(_configuration.WorkingDirectoryPath);
            var workingDirectorySecurity = workingDirectory.GetAccessControl();
            workingDirectorySecurity.AddAccessRule(new FileSystemAccessRule(
                _essentialAccessCapabilityIdentifier,
                FileSystemRights.Read,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow
            ));
            workingDirectory.SetAccessControl(workingDirectorySecurity);*/

            var windowStationHandle = User32.GetProcessWindowStation();
            var windowStationSecurity = new WindowObjectSecurity(new WindowObjectNoCloseHandle(windowStationHandle), AccessControlSections.Access);
            windowStationSecurity.AddAccessRule(new WindowObjectAccessRule(
                _essentialAccessCapabilityIdentifier,
                WindowStationRights.ReadAttributes,
                AccessControlType.Allow
            ));
            windowStationSecurity.Persist();

            var desktopHandle = User32.GetThreadDesktop(Kernel32.GetCurrentThreadId());
            var desktopSecurity = new WindowObjectSecurity(new WindowObjectNoCloseHandle(desktopHandle), AccessControlSections.Access);
            desktopSecurity.AddAccessRule(new WindowObjectAccessRule(
                _essentialAccessCapabilityIdentifier,
                DesktopRights.ReadObjects,
                AccessControlType.Allow
            ));
            desktopSecurity.Persist();

            _initialSetupCompleted = true;
        }

        public IProcessContainer StartProcess() {
            if (!_initialSetupCompleted)
                throw new InvalidOperationException("InitialSetup must be called before Run.");

            var appContainerProfile = default((string name, SafeAllocatedSID sid));

            var process = (Process?)null;
            var streams = (StandardStreams?)null;
            var jobObject = (JobObject?)null;

            var failed = false;
            try {
                streams = StandardStreams.CreateFromPipes();
                appContainerProfile = CreateAppContainerProfile();

                using var processInformation = CreateProcessInAppContainer(appContainerProfile.sid, streams!.Value);
                process = Process.GetProcessById(unchecked((int)processInformation.dwProcessId));

                jobObject = AssignProcessToJobObject(processInformation);

                ((HRESULT)Kernel32.ResumeThread(processInformation.hThread)).ThrowIfFailed();                

                return new ProcessContainer(
                    process,
                    streams!.Value,
                    jobObject,
                    appContainerProfile.name
                );
            }
            catch (Exception ex) {
                failed = true;
                SafeDispose.DisposeOnException(
                    ex,
                    (process, streams, jobObject, appContainerProfile), static x => ProcessContainer.Dispose(
                        x.process,
                        x.streams,
                        x.jobObject,
                        x.appContainerProfile.name
                    ),
                    streams, static s => s?.Dispose(),
                    streams, DisposeLocalClientHandles
                );
                throw;
            }
            finally {
                if (!failed)
                    DisposeLocalClientHandles(streams);
            }
        }

        private static void DisposeLocalClientHandles(StandardStreams? streams) {
            SafeDispose.Dispose(
                streams?.Input, static s => s?.DisposeLocalCopyOfClientHandle(),
                streams?.Output, static s => s?.DisposeLocalCopyOfClientHandle(),
                streams?.Error, static s => s?.DisposeLocalCopyOfClientHandle()
            );
        }

        private (string name, SafeAllocatedSID sid) CreateAppContainerProfile() {
            var name = "fragile-" + Guid.NewGuid().ToString("N");
            UserEnv.CreateAppContainerProfile(
                name,
                pszDisplayName: name,
                pszDescription: name,
                pCapabilities: null,
                dwCapabilityCount: 0,
                out var sid
            ).ThrowIfFailed();

            return (name, sid);
        }

        private Kernel32.SafePROCESS_INFORMATION CreateProcessInAppContainer(
            SafeAllocatedSID appContainerSid,
            StandardStreams standardStreams
        ) {
            var workingDirectoryAccessCapabilitySidHandle = default(GCHandle);
            var capabilityListHandle = default(GCHandle);

            try {
                workingDirectoryAccessCapabilitySidHandle = GCHandle.Alloc(_essentialAccessCapabilitySidBytes, GCHandleType.Pinned);
                var capability = new AdvApi32.SID_AND_ATTRIBUTES {
                    Sid = workingDirectoryAccessCapabilitySidHandle.AddrOfPinnedObject(),
                    Attributes = (uint)AdvApi32.GroupAttributes.SE_GROUP_ENABLED
                };
                capabilityListHandle = GCHandle.Alloc(capability, GCHandleType.Pinned);

                using var procThreadAttributes = CreateProcThreadAttributeList(
                    appContainerSid,
                    capabilityListHandle,
                    new HANDLE[] {
                        standardStreams.Input.ClientSafePipeHandle,
                        standardStreams.Output.ClientSafePipeHandle,
                        standardStreams.Error.ClientSafePipeHandle
                    }
                );

                var created = Kernel32.CreateProcess(
                    lpApplicationName: _exeFilePath,
                    lpCommandLine: _commandLine,
                    null,
                    null,
                    bInheritHandles: true,
                    Kernel32.CREATE_PROCESS.EXTENDED_STARTUPINFO_PRESENT
                        | Kernel32.CREATE_PROCESS.CREATE_SUSPENDED
                        /*| Kernel32.CREATE_PROCESS.DETACHED_PROCESS*/,
                    lpEnvironment: EnvironmentBlock,
                    lpCurrentDirectory: _configuration.WorkingDirectoryPath,
                    new Kernel32.STARTUPINFOEX {
                        StartupInfo = {
                            cb = (uint)Marshal.SizeOf<Kernel32.STARTUPINFOEX>(),
                            dwFlags = Kernel32.STARTF.STARTF_USESTDHANDLES,
                            hStdInput = standardStreams.Input.ClientSafePipeHandle,
                            hStdOutput = standardStreams.Output.ClientSafePipeHandle,
                            hStdError = standardStreams.Error.ClientSafePipeHandle
                        },
                        lpAttributeList = procThreadAttributes.DangerousGetHandle()
                    },
                    out var processInformation
                );
                if (!created)
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

                standardStreams.Input.DisposeLocalCopyOfClientHandle();
                standardStreams.Output.DisposeLocalCopyOfClientHandle();
                standardStreams.Error.DisposeLocalCopyOfClientHandle();

                return processInformation;
            }
            finally {
                SafeDispose.Dispose(
                    workingDirectoryAccessCapabilitySidHandle, SafeDispose.FreeGCHandle,
                    capabilityListHandle, SafeDispose.FreeGCHandle
                );
            }
        }

        private Kernel32.SafeProcThreadAttributeList CreateProcThreadAttributeList(
            SafeAllocatedSID appContainerSid,
            GCHandle capabilityHandle,
            HANDLE[] stdHandles
        ) {
            return Kernel32.SafeProcThreadAttributeList.Create(new Dictionary<Kernel32.PROC_THREAD_ATTRIBUTE, object> {
                {
                    Kernel32.PROC_THREAD_ATTRIBUTE.PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES,
                    new Kernel32.SECURITY_CAPABILITIES {
                        AppContainerSid = appContainerSid,
                        CapabilityCount = 1,
                        Capabilities = capabilityHandle.AddrOfPinnedObject()
                    }
                },
                {
                    Kernel32.PROC_THREAD_ATTRIBUTE.PROC_THREAD_ATTRIBUTE_HANDLE_LIST, stdHandles
                }
            });
        }

        private JobObject AssignProcessToJobObject(Kernel32.SafePROCESS_INFORMATION processInformation) {
            var jobObject = new JobObject(new JobLimits {
                MaximumMemory = _configuration.MaximumMemorySize,
                CpuRate = new CpuRateLimit(_configuration.MaximumCpuPercentage, true),
                ActiveProcesses = 1,
                UiRestrictions = (JobUiRestrictions)0xFFFFFF
            });
            jobObject.AssignProcess(processInformation.hProcess.DangerousGetHandle());

            return jobObject;
        }
    }
}
