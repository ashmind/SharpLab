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

        public static bool HasCachingSeenSlowUpdateBefore(this IWorkSession session) {
            return (bool?)session.ExtensionData.GetValueOrDefault("CachingHasSeenSlowUpdate") ?? false;
        }

        public static void SetCachingHasSeenSlowUpdate(this IWorkSession session) {
            session.ExtensionData["CachingHasSeenSlowUpdate"] = true;
        }
    }
}