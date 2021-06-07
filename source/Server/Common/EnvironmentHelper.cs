using System;

namespace SharpLab.Server.Common {
    public static class EnvironmentHelper {
        public static string GetRequiredEnvironmentVariable(string name) {
            return Environment.GetEnvironmentVariable(name)
                ?? throw new Exception($"Environment variable {name} was not found");
        }
    }
}
