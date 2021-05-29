using System;
using System.Collections.Generic;

namespace SharpLab.Runtime.Internal {
    public static class RuntimeServices {
        #pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public static IValuePresenter ValuePresenter { get; set; }
        public static IMemoryBytesInspector MemoryBytesInspector { get; set; }
        public static IAllocationInspector AllocationInspector { get; set; }
        public static Func<IReadOnlyList<string>, IMemoryGraphBuilder> MemoryGraphBuilderFactory { get; set; }
        internal static IInspectionWriter? InspectionWriter { get; set; }
        internal static IFlowWriter FlowWriter { get; set; }
        #pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
