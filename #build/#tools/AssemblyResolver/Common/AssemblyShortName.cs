using System;
using JetBrains.Annotations;

namespace AssemblyResolver.Common {
    public struct AssemblyShortName : IEquatable<AssemblyShortName>, IComparable<AssemblyShortName> {
        private static readonly StringComparer StringComparer = StringComparer.OrdinalIgnoreCase;

        public AssemblyShortName([NotNull] string name) {
            Name = name;
        }

        [NotNull] public string Name { get; }

        public bool Equals(AssemblyShortName other) => StringComparer.Equals(Name, other.Name);
        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            return obj is AssemblyShortName
                && Equals((AssemblyShortName)obj);
        }

        public override int GetHashCode() {
            return StringComparer.GetHashCode(Name);
        }

        public static implicit operator AssemblyShortName([NotNull] string name) => new AssemblyShortName(name);

        public int CompareTo(AssemblyShortName other) {
            return StringComparer.Compare(Name, other.Name);
        }

        public override string ToString() => Name;
    }
}