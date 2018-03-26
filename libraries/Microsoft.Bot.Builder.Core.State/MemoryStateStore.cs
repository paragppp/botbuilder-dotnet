using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.State
{
    public class MemoryStateStore : IStateStore
    {
        private Dictionary<string, Dictionary<string, IStateStoreEntry>> _store = new Dictionary<string, Dictionary<string, IStateStoreEntry>>();

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

            if(_store.TryGetValue(partitionKey, out var entriesForPartion))
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
                    if(entriesForPartion.TryGetValue(key, out var entry))
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

            if(!_store.TryGetValue(partitionKey, out var entriesForPartition))
            {
                entriesForPartition = new Dictionary<string, IStateStoreEntry>();

                _store.Add(partitionKey, entriesForPartition);
            }

            foreach (var entry in values)
            {
                entriesForPartition.Add(entry.Key, new StateStoreEntry(partitionKey, entry.Key, entry.Value));

            }

            return Task.CompletedTask;
        }

        public Task Save(IEnumerable<IStateStoreEntry> entries)
        {
            foreach (var entityGroup in entries.GroupBy(e => e.Namespace))
            {
                if (!_store.TryGetValue(entityGroup.Key, out var entriesForPartition))
                {
                    entriesForPartition = new Dictionary<string, IStateStoreEntry>();

                    _store.Add(entityGroup.Key, entriesForPartition);
                }

                foreach (var entry in entries)
                {
                    if(!(entry is StateStoreEntry stateStoreEntry))
                    {
                        throw new ArgumentException($"Specified value is not of type {nameof(StateStoreEntry)}.");
                    }

                    if (entriesForPartition.TryGetValue(stateStoreEntry.Key, out var existingEntry))
                    {
                        if(!Object.ReferenceEquals(entry, existingEntry))
                        {
                            if (stateStoreEntry.ETag != existingEntry.ETag)
                            {
                                throw new StateOptimisticConcurrencyViolation($"An optimistic concurrency violation occurred when trying to save state for: PartitionKey={stateStoreEntry.Namespace};Key={stateStoreEntry.Key}. The original ETag value was {stateStoreEntry.ETag}, but the current ETag value is {existingEntry.ETag}.");
                            }
                        }
                    }

                    stateStoreEntry.ETag = Guid.NewGuid().ToString("N");
                    entriesForPartition[stateStoreEntry.Key] = stateStoreEntry;                    
                }
            }

            return Task.CompletedTask;
        }

        public Task Delete(string partitionKey)
        {
            _store.Remove(partitionKey);

            return Task.CompletedTask;
        }

        public Task Delete(string partitionKey, string key)
        {
            if(_store.TryGetValue(partitionKey, out var entriesForPartition))
            {
                entriesForPartition.Remove(key);
            }

            return Task.CompletedTask;
        }

        public Task Delete(string partitionKey, IEnumerable<string> keys)
        {
            if (_store.TryGetValue(partitionKey, out var entriesForPartition))
            {
                foreach (string key in keys)
                {
                    entriesForPartition.Remove(key);
                }
            }

            return Task.CompletedTask;
        }
    }
}