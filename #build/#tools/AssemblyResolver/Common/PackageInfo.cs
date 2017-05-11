namespace AssemblyResolver.Common {
    public class PackageInfo {
        private readonly string _sourceFilePath;

        public PackageInfo(string packageId, string packageVersion, string sourceFilePath) {
            PackageId = packageId;
            PackageVersion = packageVersion;
            _sourceFilePath = sourceFilePath;
        }
        public string PackageId { get; }
        public string PackageVersion { get; }

        public override bool Equals(object obj) {
            return Equals(obj as PackageInfo);
        }

        public bool Equals(PackageInfo other) {
            if (other == null)
                return false;
            return PackageId == other.PackageId
                && PackageVersion == other.PackageVersion;
        }

        public override int GetHashCode() {
            return PackageId.GetHashCode()
                 ^ PackageVersion.GetHashCode();
        }

        public override string ToString() => $"{PackageId}.{PackageVersion} ({_sourceFilePath})";
    }
}