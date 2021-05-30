using System;

namespace SharpLab.Container.Manager.Internal {
    public class ContainerAllocationException : Exception {
        public ContainerAllocationException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}
