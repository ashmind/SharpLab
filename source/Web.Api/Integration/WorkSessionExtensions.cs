using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AshMind.Extensions;
using MirrorSharp.Advanced;
using TryRoslyn.Core;

namespace TryRoslyn.Web.Api.Integration {
    public static class WorkSessionExtensions {
        public static LanguageIdentifier? GetTargetLanguage(this IWorkSession session) {
            return (LanguageIdentifier?)session.ExtensionData.GetValueOrDefault("TargetLanguage");
        }

        public static void SetTargetLanguage(this IWorkSession session, LanguageIdentifier value) {
            session.ExtensionData["TargetLanguage"] = value;
        }
    }
}