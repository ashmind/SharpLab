using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MirrorSharp.Advanced;
using SharpLab.Server.Caching;
using SharpLab.Server.Common;

namespace SharpLab.Server.MirrorSharp {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class SetOptionsFromClient : ISetOptionsFromClientExtension {
        private const string Optimize = "x-optimize";
        private const string Target = "x-target";
        private const string NoCache = "x-no-cache";

        private readonly IDictionary<string, ILanguageAdapter> _languages;

        public SetOptionsFromClient(IReadOnlyList<ILanguageAdapter> languages) {
            _languages = languages.ToDictionary(l => l.LanguageName);
        }

        public bool TrySetOption(IWorkSession session, string name, string value) {
            switch (name) {
                case Optimize:
                    session.SetOptimize(value);
                    _languages[session.LanguageName].SetOptimize(session, value);
                    return true;
                case Target:
                    session.SetTargetName(value);
                    _languages[session.LanguageName].SetOptionsForTarget(session, value);
                    return true;
                case NoCache:
                    if (value != "true")
                        throw new NotSupportedException("Option 'no-cache' can only be set to true.");
                    // Mostly used to avoid caching on the first change after a cached result was loaded
                    session.SetCachingDisabled(true);
                    return true;
                default:
                    #if !DEBUG
                    // Need to allow unknown options for future compatibility
                    return true;
                    #else
                    return false;
                    #endif
            }
        }
    }
}