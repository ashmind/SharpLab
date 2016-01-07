using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace TryRoslyn.Core {
    [Serializable]
    public class ProcessingResultDiagnosticLocation {
        public ProcessingResultDiagnosticLocation(LinePosition position) {
            Line = position.Line;
            Column = position.Character;
        }

        public int Line { get; private set; }
        public int Column { get; private set; }
    }
}
