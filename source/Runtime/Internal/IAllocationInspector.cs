using System;

namespace SharpLab.Runtime.Internal {
    public interface IAllocationInspector {
        IInspection InspectAllocations(Action action);
    }
}