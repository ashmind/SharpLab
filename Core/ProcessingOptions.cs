using System;
using System.Collections.Generic;
using System.Linq;

namespace TryRoslyn.Core {
    [Serializable]
    public class ProcessingOptions {
        public bool OptimizationsEnabled { get; set; }
        public bool ScriptMode { get; set; }
        public LanguageIdentifier Language { get; set; }
    }
}
