using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using SharpLab.Server.Common;

namespace SharpLab.Server.Integration.Azure {
    public class TableStorageFeatureFlagClient : IFeatureFlagClient {
        private readonly CloudTable _cloudTable;
        private readonly IReadOnlyDictionary<string, FlagCacheEntry> _cache;
        private readonly ILogger<TableStorageFeatureFlagClient> _logger;

        public TableStorageFeatureFlagClient(
            CloudTable cloudTable,
            IReadOnlyList<string> flagKeys,
            ILogger<TableStorageFeatureFlagClient> logger
        ) {
            _cloudTable = cloudTable;
            _logger = logger;

            _cache = flagKeys.ToDictionary(k => k, k => new FlagCacheEntry(k));
        }

        public void Start() {
            foreach (var entry in _cache.Values) {
                entry.GetOrStartUpdate(new() { Task = UpdateCacheValueAsync(entry) });
            }
        }

        public int? GetInt32Flag(string key) {
            var cacheEntry = _cache[key];
            var cachedValue = cacheEntry.Value;
            if (cachedValue == null)
                return TryUpdateAndGetValue(cacheEntry)?.Int32Value;

            if ((DateTime.Now - cacheEntry.LastUpdatedAt).TotalMinutes > 1)
                return TryUpdateAndGetValue(cacheEntry)!.Int32Value;

            return cachedValue.Int32Value;
        }

        private EntityProperty? TryUpdateAndGetValue(FlagCacheEntry cacheEntry) {
            if (cacheEntry.NextUpdateRetryTime > DateTime.Now)
                return cacheEntry.Value;

            var newUpdate = new Update();
            var update = cacheEntry.GetOrStartUpdate(newUpdate);
            if (update == newUpdate)
                update.Task = UpdateCacheValueAsync(cacheEntry);

            update.Task?.Wait(100);
            return cacheEntry.Value;
        }

        private async Task UpdateCacheValueAsync(FlagCacheEntry cacheEntry) {
            try {
                _logger.LogDebug("Updating cache for flag {0}.", cacheEntry.Key);
                var result = await _cloudTable.ExecuteAsync(
                    TableOperation.Retrieve<DynamicTableEntity>("default", cacheEntry.Key)
                );
                var value = ((DynamicTableEntity)result.Result).Properties["Value"];

                cacheEntry.CompleteUpdate(value);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to update cache value for flag {0}.", cacheEntry.Key);
                cacheEntry.FailUpdateAndRetryAt(DateTime.Now.AddMinutes(5));
            }
        }

        private class FlagCacheEntry {
            private Update? _currentUpdate;

            public FlagCacheEntry(string key) {
                Key = key;
            }

            public string Key { get; }
            public EntityProperty? Value { get; private set; }
            public DateTime LastUpdatedAt { get; private set; }
            public DateTime? NextUpdateRetryTime { get; private set; }
            public Update? CurrentUpdate => _currentUpdate;

            public Update GetOrStartUpdate(Update candidate) {
                return Interlocked.CompareExchange(ref _currentUpdate, candidate, null) ?? candidate;
            }

            public void CompleteUpdate(EntityProperty value) {
                Value = value;
                LastUpdatedAt = DateTime.Now;
                NextUpdateRetryTime = null;
                _currentUpdate = null;
            }

            public void FailUpdateAndRetryAt(DateTime nextUpdateRetryTime) {
                NextUpdateRetryTime = nextUpdateRetryTime;
                _currentUpdate = null;
            }
        }

        private class Update {
            public Task? Task { get; set; }
        }
    }
}
