namespace SharpLab.Server.Monitoring {
    public class MonitorMetric {
        public MonitorMetric(string @namespace, string name) {
            Argument.NotNullOrEmpty(nameof(@namespace), @namespace);
            Argument.NotNullOrEmpty(nameof(name), name);

            Namespace = @namespace;
            Name = name;
        }

        public string Namespace { get; }
        public string Name { get; }
    }
}
