using System;
using AshMind.Extensions;
using MirrorSharp.Advanced;

namespace SharpLab.Server.MirrorSharp {
    public static class WorkSessionExtensions {
        public static string? GetTargetName(this IWorkSession session) {
            return (string?)session.ExtensionData.GetValueOrDefault("TargetName");
        }

        public static void SetTargetName(this IWorkSession session, string value) {
            session.ExtensionData["TargetName"] = value;
        }

        public static string GetSessionId(this IWorkSession session) {
            var id = (string?)session.ExtensionData.GetValueOrDefault("SessionId");
            if (id == null) {
                id = Guid.NewGuid().ToString();
                session.ExtensionData["SessionId"] = id;
            }
            return id;
        }
    }
}