namespace SharpLab.Runtime.Internal {
    public static class InspectionSettings {
        public static bool ProfilerActive { get; set; }
        public static int CurrentProcessId { get; set; }
        public static ulong StackStart { get; set; }
    }
}
