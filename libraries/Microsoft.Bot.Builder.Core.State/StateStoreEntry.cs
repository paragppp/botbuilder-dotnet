using System;

namespace Microsoft.Bot.Builder.Core.State
{
    public class StateStoreEntry : IStateStoreEntry
    {
        private string _namespace;
        private string _key;
        private string _eTag;
        private object _value;

        public StateStoreEntry(string stateNamespace, string key)
        {
            _namespace = stateNamespace ?? throw new ArgumentNullException(nameof(stateNamespace));
            _key = key ?? throw new ArgumentNullException(nameof(key));

        }

        public StateStoreEntry(string stateNamespace, string key, object value) : this(stateNamespace, key)
        {
            _value = value;
        }

        public StateStoreEntry(string stateNamespace, string key, string eTag) : this(stateNamespace, key)
        {
            _eTag = eTag ?? throw new ArgumentNullException(nameof(eTag));
        }

        public StateStoreEntry(string stateNamespace, string key, string eTag, object value) : this(stateNamespace, key, eTag)
        {
            _value = value;
        }

        public string Namespace => _namespace;

        public string Key => _key;

        public string ETag
        {
            get => _eTag;
            protected internal set
            {
                _eTag = value;
            }
        }

        public object RawValue
        {
            get => _value;
            protected internal set
            {
                _value = value;
            }
        }

        public virtual T GetValue<T>() where T : class => (T)_value;

        public virtual void SetValue<T>(T value) where T : class => _value = value;
    }
}