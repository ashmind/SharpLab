using System;

namespace Fragile {
    public class ProcessRunnerConfiguration {
        public ProcessRunnerConfiguration(
            string workingDirectoryPath,
            string exeFileName,
            string workingDirectoryAccessCapabilitySid,
            ulong maximumMemorySize,
            decimal maximumCpuPercentage
        ) {
            Argument.NotNullOrEmpty(nameof(workingDirectoryPath), workingDirectoryPath);
            Argument.NotNullOrEmpty(nameof(exeFileName), exeFileName);
            Argument.NotNullOrEmpty(nameof(workingDirectoryAccessCapabilitySid), workingDirectoryAccessCapabilitySid);
            if (exeFileName.Contains(" "))
                throw new ArgumentException("Spaces in exe filename are not supported.", nameof(exeFileName));

            WorkingDirectoryPath = workingDirectoryPath;
            ExeFileName = exeFileName;
            WorkingDirectoryAccessCapabilitySid = workingDirectoryAccessCapabilitySid;
            MaximumMemorySize = maximumMemorySize;
            MaximumCpuPercentage = maximumCpuPercentage;
        }

        public string WorkingDirectoryPath { get; }
        public string ExeFileName { get; }
        public string WorkingDirectoryAccessCapabilitySid { get; }
        public ulong MaximumMemorySize { get; }
        public decimal MaximumCpuPercentage { get; }
    }
}
