namespace SharpLab.Server.Execution {
    public class ContainerExecutionResult {
        public ContainerExecutionResult(string output, bool outputFailed) {
            Output = output;
            OutputFailed = outputFailed;
        }

        public string Output { get; }
        public bool OutputFailed { get; }
    }
}
