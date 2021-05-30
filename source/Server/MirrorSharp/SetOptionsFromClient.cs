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
        private const string ContainerExperimentKey = "x-container-experiment";

        private readonly IDictionary<string, ILanguageAdapter> _languages;
        private readonly ContainerExperimentSettings _containerExperimentSettings;

        public SetOptionsFromClient(IReadOnlyList<ILanguageAdapter> languages, ContainerExperimentSettings containerExperimentSettings) {
            _languages = languages.ToDictionary(l => l.LanguageName);
            _containerExperimentSettings = containerExperimentSettings;
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
                case ContainerExperimentKey:
                    session.SetContainerExperimentAllowed(value == _containerExperimentSettings.AccessKey);
                    return true;
                default:
                    return false;
            }
        }
    }
}