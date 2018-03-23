using System;

namespace Microsoft.Bot.Builder.Core.State
{
    public interface IStateStoreEntry
    {
        string PartitionKey { get; }

        string Key { get;  }

        string ETag { get; }

        T GetValue<T>() where T : class;

        void SetValue<T>(T value) where T : class;
    }

    public abstract class StateStoreEntry : IStateStoreEntry
    {
        private string _partitionKey;
        private string _key;
        private string _eTag;

        public StateStoreEntry(string partitionKey, string key)
        {
            _partitionKey = partitionKey ?? throw new ArgumentNullException(nameof(partitionKey));
            _key = key ?? throw new ArgumentNullException(nameof(key));

        }

        public StateStoreEntry(string partitionKey, string key, string eTag) : this(partitionKey, key)
        {
            _eTag = eTag ?? throw new ArgumentNullException(nameof(eTag));
        }

        public string PartitionKey => _partitionKey;

        public string Key => _key;

        public string ETag
        {
            get => _eTag;
            protected internal set
            {
                _eTag = value;
            }
        }

        public abstract T GetValue<T>() where T : class;

        public abstract void SetValue<T>(T value) where T : class;
    }
}