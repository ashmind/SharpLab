using AshMind.Extensions;
using MirrorSharp.Advanced;

namespace SharpLab.Server.MirrorSharp {
    public static class WorkSessionExtensions {
        public static string GetTargetLanguageName(this IWorkSession session) {
            return (string)session.ExtensionData.GetValueOrDefault("TargetLanguageName");
        }

        public static void SetTargetLanguageName(this IWorkSession session, string value) {
            session.ExtensionData["TargetLanguageName"] = value;
        }
    }
}