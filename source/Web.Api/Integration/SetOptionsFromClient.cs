using System;
using MirrorSharp.Advanced;
using TryRoslyn.Core;

namespace TryRoslyn.Web.Api.Integration {
    public class SetOptionsFromClient : ISetOptionsFromClientExtension {
        private const string TargetLanguage = "x-target-language";

        public bool TrySetOption(IWorkSession session, string name, string value) {
            if (name != TargetLanguage)
                return false;

            session.SetTargetLanguage((LanguageIdentifier)Enum.Parse(typeof(LanguageIdentifier), value, true));
            return true;
        }
    }
}