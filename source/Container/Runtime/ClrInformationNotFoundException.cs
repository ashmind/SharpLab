using System;

namespace SharpLab.Container.Runtime {
    internal class ClrInformationNotFoundException : Exception {
        public ClrInformationNotFoundException(string message) : base(message) {
        }
    }
}
