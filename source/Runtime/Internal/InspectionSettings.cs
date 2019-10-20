using System.Collections.Generic;
using System.Threading;

namespace SharpLab.Runtime.Internal {
    public class InspectionSettings {
        private static readonly AsyncLocal<InspectionSettings> _current = new AsyncLocal<InspectionSettings>();

        public static InspectionSettings Current {
            get => _current.Value;
            set => _current.Value = value;
        }

        public InspectionSettings(int currentProcessId, bool profilerActive) {
            CurrentProcessId = currentProcessId;
            ProfilerActive = profilerActive;
        }

        public int CurrentProcessId { get; }
        public bool ProfilerActive { get; }
        public ulong StackStart { get; set; }
    }
}
