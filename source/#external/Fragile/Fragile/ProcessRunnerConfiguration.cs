using System;

namespace Fragile {
    public class ProcessRunnerConfiguration {
        public ProcessRunnerConfiguration(
            string workingDirectoryPath,
            string exeFileName,
            string essentialAccessCapabilitySid,
            ulong maximumMemorySize,
            decimal maximumCpuPercentage
        ) {
            Argument.NotNullOrEmpty(nameof(workingDirectoryPath), workingDirectoryPath);
            Argument.NotNullOrEmpty(nameof(exeFileName), exeFileName);
            Argument.NotNullOrEmpty(nameof(essentialAccessCapabilitySid), essentialAccessCapabilitySid);
            if (exeFileName.Contains(" "))
                throw new ArgumentException("Spaces in exe filename are not supported.", nameof(exeFileName));

            WorkingDirectoryPath = workingDirectoryPath;
            ExeFileName = exeFileName;
            EssentialAccessCapabilitySid = essentialAccessCapabilitySid;
            MaximumMemorySize = maximumMemorySize;
            MaximumCpuPercentage = maximumCpuPercentage;
        }

        public string WorkingDirectoryPath { get; }
        public string ExeFileName { get; }

        /// <summary>
        /// Capability SID to be automatically granted access that
        /// is absolutely required for process to function. Should
        /// have at least read access to working directory, as
        /// that cannot be granted dynamically.
        /// </summary>
        public string EssentialAccessCapabilitySid { get; }

        public ulong MaximumMemorySize { get; }
        public decimal MaximumCpuPercentage { get; }
    }
}
