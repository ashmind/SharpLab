using System;
using System.Collections.Generic;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class InspectionGroup : IInspection {
        public InspectionGroup(string title, IReadOnlyList<IInspection> inspections, bool limitReached) {
            Title = title;
            Inspections = inspections;
            LimitReached = limitReached;
        }

        public string Title { get; }
        public IReadOnlyList<IInspection> Inspections { get; }
        public bool LimitReached { get; }
    }
}
