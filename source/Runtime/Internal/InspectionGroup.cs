using System;
using System.Collections.Generic;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class InspectionGroup : IInspection {
        public InspectionGroup(string title, IReadOnlyList<IInspection> inspections) {
            Title = title;
            Inspections = inspections;
        }

        public string Title { get; }
        public IReadOnlyList<IInspection> Inspections { get; }
    }
}
