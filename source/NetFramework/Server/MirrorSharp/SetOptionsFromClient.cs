using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;

namespace SharpLab.Server.MirrorSharp {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class SetOptionsFromClient : ISetOptionsFromClientExtension {
        private const string Optimize = "x-optimize";
        private const string Target = "x-target";
        private const string ContainerExperimentSeed = "x-container-experiment-seed";

        private readonly IDictionary<string, ILanguageAdapter> _languages;

        public SetOptionsFromClient(IReadOnlyList<ILanguageAdapter> languages) {
            _languages = languages.ToDictionary(l => l.LanguageName);
        }

        public bool TrySetOption(IWorkSession session, string name, string value) {
            switch (name) {
                case Optimize:
                    _languages[session.LanguageName].SetOptimize(session, value);
                    return true;
                case Target:
                    session.SetTargetName(value);
                    _languages[session.LanguageName].SetOptionsForTarget(session, value);
                    return true;
                case ContainerExperimentSeed:
                    // TODO: remove once UI logic is removed (not supported in .NET Framework either way)
                    return true;
                default:
                    return false;
            }
        }
    }
}