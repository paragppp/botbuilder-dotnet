using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.State
{
    public class MemoryStateStore : IStateStore
    {
        private ConcurrentDictionary<string, ConcurrentDictionary<string, IStateStoreEntry>> _store = new ConcurrentDictionary<string, ConcurrentDictionary<string, IStateStoreEntry>>();

        public MemoryStateStore()
        {
        }

        public IStateStoreEntry CreateNewStateEntry(string partitionKey, string key) => new StateStoreEntry(partitionKey, key);

        public Task<IEnumerable<IStateStoreEntry>> Load(string partitionKey)
        {
            if (partitionKey == null)
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            if (_store.TryGetValue(partitionKey, out var entriesForPartion))
            {
                return Task.FromResult(entriesForPartion.Select(kvp => kvp.Value));
            }

            return Task.FromResult(Enumerable.Empty<IStateStoreEntry>());
        }

        public Task<IStateStoreEntry> Load(string partitionKey, string key)
        {
            if (partitionKey == null)
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_store.TryGetValue(partitionKey, out var entriesForPartion))
            {
                if (entriesForPartion.TryGetValue(key, out var entry))
                {
                    return Task.FromResult(entry);
                }
            }

            return Task.FromResult(default(IStateStoreEntry));
        }

        public Task<IEnumerable<IStateStoreEntry>> Load(string partitionKey, IEnumerable<string> keys)
        {
            if (partitionKey == null)
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            if (_store.TryGetValue(partitionKey, out var entriesForPartion))
            {
                var results = new List<IStateStoreEntry>(entriesForPartion.Count);

                foreach (var key in keys)
                {
                    if (entriesForPartion.TryGetValue(key, out var entry))
                    {
                        results.Add(entry);
                    }
                }

                return Task.FromResult<IEnumerable<IStateStoreEntry>>(results);
            }

            return Task.FromResult(Enumerable.Empty<IStateStoreEntry>());
        }

        public Task Save(string partitionKey, IEnumerable<KeyValuePair<string, object>> values)
        {
            if (partitionKey == null)
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            var entriesForPartition = _store.GetOrAdd(partitionKey, pk => new ConcurrentDictionary<string, IStateStoreEntry>());

            foreach (var entry in values)
            {
                var newStateStoreEntry = new StateStoreEntry(partitionKey, entry.Key, entry.Value);

                entriesForPartition.AddOrUpdate(
                    entry.Key,
                    newStateStoreEntry,
                    (key, existingStateStoreEntry) =>
                    {
                        ThrowIfStateStoreEntryETagMismatch(newStateStoreEntry, existingStateStoreEntry);

                        return newStateStoreEntry;
                    });
            }

            return Task.CompletedTask;
        }

        public Task Save(IEnumerable<IStateStoreEntry> entries)
        {
            foreach (var entityGroup in entries.GroupBy(e => e.Namespace))
            {
                var entriesForPartition = _store.GetOrAdd(entityGroup.Key, pk => new ConcurrentDictionary<string, IStateStoreEntry>());

                foreach (var entry in entries)
                {
                    if (!(entry is StateStoreEntry stateStoreEntry))
                    {
                        throw new ArgumentException($"Only instances of {nameof(StateStoreEntry)} are supported by {nameof(MemoryStateStore)}.");
                    }

                    if (entriesForPartition.TryGetValue(stateStoreEntry.Key, out var existingEntry))
                    {
                        ThrowIfStateStoreEntryETagMismatch(stateStoreEntry, existingEntry);
                    }

                    entriesForPartition[stateStoreEntry.Key] = new StateStoreEntry(stateStoreEntry.Namespace, stateStoreEntry.Key, Guid.NewGuid().ToString("N"), stateStoreEntry.RawValue);
                }
            }

            return Task.CompletedTask;
        }

        public Task Delete(string partitionKey)
        {
            _store.TryRemove(partitionKey, out _);

            return Task.CompletedTask;
        }

        public Task Delete(string partitionKey, string key)
        {
            if (_store.TryGetValue(partitionKey, out var entriesForPartition))
            {
                entriesForPartition.TryRemove(key, out _);
            }

            return Task.CompletedTask;
        }

        public Task Delete(string partitionKey, IEnumerable<string> keys)
        {
            if (_store.TryGetValue(partitionKey, out var entriesForPartition))
            {
                foreach (string key in keys)
                {
                    entriesForPartition.TryRemove(key, out _);
                }
            }

            return Task.CompletedTask;
        }

        private static void ThrowIfStateStoreEntryETagMismatch(IStateStoreEntry newEntry, IStateStoreEntry existingEntry)
        {
            if (!Object.ReferenceEquals(newEntry, existingEntry))
            {
                if (newEntry.ETag != existingEntry.ETag)
                {
                    throw new StateOptimisticConcurrencyViolationException($"An optimistic concurrency violation occurred when trying to save new state for: PartitionKey={newEntry.Namespace};Key={newEntry.Key}. The original ETag value was {newEntry.ETag}, but the current ETag value is {existingEntry.ETag}.");
                }
            }
        }

    }
}