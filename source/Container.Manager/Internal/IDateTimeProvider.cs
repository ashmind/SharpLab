using System;

namespace SharpLab.Container.Manager.Internal {
    public interface IDateTimeProvider {
        DateTimeOffset GetNow();
    }
}
