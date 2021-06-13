using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;
using SharpLab.Server.Execution.Container;

namespace SharpLab.Server.MirrorSharp {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class SetOptionsFromClient : ISetOptionsFromClientExtension {
        private const string Optimize = "x-optimize";
        private const string Target = "x-target";
        private const string ContainerExperimentSeedKey = "x-container-experiment-seed";

        private readonly IDictionary<string, ILanguageAdapter> _languages;
        private readonly IFeatureFlagClient _featureFlagClient;

        public SetOptionsFromClient(
            IReadOnlyList<ILanguageAdapter> languages,
            IFeatureFlagClient featureFlagClient
        ) {
            _languages = languages.ToDictionary(l => l.LanguageName);
            _featureFlagClient = featureFlagClient;
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
                case ContainerExperimentSeedKey:
                    session.SetInContainerExperiment(
                        int.Parse(value) <= (_featureFlagClient.GetInt32Flag("ContainerExperimentRollout") ?? 0)
                    );
                    return true;
                default:
                    return false;
            }
        }
    }
}