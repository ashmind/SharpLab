using System;

namespace SharpLab.Container.Manager.Internal {
    public class DateTimeProvider : IDateTimeProvider {
        public DateTimeOffset GetNow() => DateTimeOffset.Now;
    }
}
