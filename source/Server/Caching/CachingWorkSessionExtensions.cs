using AshMind.Extensions;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Caching {
    public static class CachingWorkSessionExtensions {
        public static bool IsCachingDisabled(this IWorkSession session) {
            return (bool?)session.ExtensionData.GetValueOrDefault("CachingDisabled") ?? false;
        }

        public static void SetCachingDisabled(this IWorkSession session, bool value) {
            session.ExtensionData["CachingDisabled"] = value;
        }

        public static bool WasFirstSlowUpdateCached(this IWorkSession session) {
            return (bool?)session.ExtensionData.GetValueOrDefault("FirstSlowUpdateCached") ?? false;
        }

        public static void SetFirstSlowUpdateCached(this IWorkSession session, bool value) {
            session.ExtensionData["FirstSlowUpdateCached"] = value;
        }
    }
}