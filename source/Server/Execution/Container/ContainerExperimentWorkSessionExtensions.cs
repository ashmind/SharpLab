using AshMind.Extensions;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Execution.Container {
    public static class ContainerExperimentWorkSessionExtensions {
        public static bool GetContainerExperimentAccessAllowed(this IWorkSession session) {
            return (bool?)session.ExtensionData.GetValueOrDefault("ContainerExperimentAccessAllowed") ?? false;
        }

        public static void SetContainerExperimentAccessAllowed(this IWorkSession session, bool value) {
            session.ExtensionData["ContainerExperimentAccessAllowed"] = value;
        }
    }
}