using System;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SharpLab.Container.Manager.Internal {
    public class ContainerPool {
        private readonly Channel<ActiveContainer> _preallocated = Channel.CreateBounded<ActiveContainer>(new BoundedChannelOptions(2) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true
        });
        private readonly MemoryCache _sessionCache = new("_");
        private readonly IDateTimeProvider _dateTimeProvider;
        private Exception? _lastContainerPreallocationException;

        public ContainerPool(IDateTimeProvider dateTimeProvider) {
            _dateTimeProvider = dateTimeProvider;
        }

        public ChannelWriter<ActiveContainer> PreallocatedContainersWriter => _preallocated.Writer;
        public Exception? LastContainerPreallocationException {
            get => _lastContainerPreallocationException;
            set {
                _lastContainerPreallocationException = value;
                ContainerPreallocationFailingSince = value != null
                    ? (ContainerPreallocationFailingSince ?? _dateTimeProvider.GetNow())
                    : null;
            }
        }
        public DateTimeOffset? ContainerPreallocationFailingSince { get; private set; }

        public async ValueTask<ActiveContainer> AllocateSessionContainerAsync(
            string sessionId, Action<ActiveContainer> scheduleCleanup, CancellationToken cancellationToken
        ) {
            var container = await _preallocated.Reader.ReadAsync(cancellationToken);
            try {
                _sessionCache.Set(sessionId, container, GetSessionContainerCachePolicy(scheduleCleanup));
                return container;
            }
            catch {
                scheduleCleanup(container);
                throw;
            }
        }

        private CacheItemPolicy GetSessionContainerCachePolicy(Action<ActiveContainer> scheduleCleanup) => new() {
            AbsoluteExpiration = DateTime.Now.AddMinutes(5),
            RemovedCallback = c => scheduleCleanup((ActiveContainer)c.CacheItem.Value)
        };

        public ActiveContainer? GetSessionContainer(string sessionId) {
            return _sessionCache.Get(sessionId) as ActiveContainer;
        }

        public void RemoveSessionContainer(string sessionId) {
            _sessionCache.Remove(sessionId);
        }
    }
}
