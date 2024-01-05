using System;
using AshMind.Extensions;
using MirrorSharp.Advanced;

namespace SharpLab.Server.MirrorSharp;

public static class WorkSessionExtensions {
    public static string? GetTargetName(this IWorkSession session) {
        return (string?)session.ExtensionData.GetValueOrDefault("TargetName");
    }

    public static void SetTargetName(this IWorkSession session, string value) {
        session.ExtensionData["TargetName"] = value;
    }

    public static string? GetOptimize(this IWorkSession session) {
        return (string?)session.ExtensionData.GetValueOrDefault("Optimize");
    }

    public static void SetOptimize(this IWorkSession session, string value) {
        session.ExtensionData["Optimize"] = value;
    }

    public static bool ShouldReportPerformance(this IWorkSession session) {
        return (bool?)session.ExtensionData.GetValueOrDefault("DebugIncludePerformance") ?? false;
    }

    public static void SetShouldReportPerformance(this IWorkSession session, bool value) {
        session.ExtensionData["DebugIncludePerformance"] = value;
    }

    public static object? GetLastSlowUpdateResult(this IWorkSession session) {
        return session.ExtensionData.GetValueOrDefault("LastSlowUpdateResult");
    }

    public static void SetLastSlowUpdateResult(this IWorkSession session, object? value) {
        session.ExtensionData["LastSlowUpdateResult"] = value;
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