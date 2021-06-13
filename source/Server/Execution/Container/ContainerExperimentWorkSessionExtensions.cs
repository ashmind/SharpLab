using System;
using AshMind.Extensions;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Execution.Container {
    public static class ContainerExperimentWorkSessionExtensions {
        public static bool InContainerExperiment(this IWorkSession session) {
            return (bool?)session.ExtensionData.GetValueOrDefault("InContainerExperiment") ?? false;
        }

        public static void SetInContainerExperiment(this IWorkSession session, bool value) {
            session.ExtensionData["InContainerExperiment"] = value;
        }

        public static bool HasContainerExperimentFailed(this IWorkSession session) {
            return session.GetContainerExperimentException() != null;
        }

        public static Exception? GetContainerExperimentException(this IWorkSession session) {
            return (Exception?)session.ExtensionData.GetValueOrDefault("ContainerExperimentException");
        }

        public static void SetContainerExperimentException(this IWorkSession session, Exception exception) {
            session.ExtensionData["ContainerExperimentException"] = exception;
        }

        public static bool WasContainerExperimentExceptionReported(this IWorkSession session) {
            return (bool?)session.ExtensionData.GetValueOrDefault("ContainerExperimentExceptionReported") ?? false;
        }

        public static void SetContainerExperimentExceptionReported(this IWorkSession session, bool value) {
            session.ExtensionData["ContainerExperimentExceptionReported"] = true;
        }
    }
}