﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.State
{
    public class StateManager : IStateManager
    {
        private readonly Dictionary<string, StateEntry> _state = new Dictionary<string, StateEntry>();

        public StateManager(string stateNamespace, IStateStore stateStore)
        {
            Namespace = stateNamespace ?? throw new ArgumentNullException(nameof(stateNamespace));
            StateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        }

        public string Namespace { get; }

        public IStateStore StateStore { get; }

        public async Task<TState> Get<TState>(string key) where TState : class, new()
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (!_state.TryGetValue(key, out var stateEntry))
            {
                var stateStoreEntry = await StateStore.Load(Namespace, key);

                if (stateStoreEntry == default(IStateStoreEntry))
                {
                    return default(TState);
                }

                _state.Add(key, new StateEntry(stateStoreEntry));

                return stateStoreEntry.GetValue<TState>();
            }

            return stateEntry.IsPendingDeletion ? default(TState) : stateEntry.StateStoreEntry.GetValue<TState>();
        }

        public void Set<TState>(string key, TState state) where TState : class
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (!_state.TryGetValue(key, out var stateEntry))
            {
                var stateStoreEntry = StateStore.CreateNewStateEntry(Namespace, key);

                stateEntry = new StateEntry(stateStoreEntry, isDirty: true);

                _state.Add(key, stateEntry);
            }
            else
            {
                stateEntry.IsDirty = true;
                stateEntry.IsPendingDeletion = false;
            }

            stateEntry.StateStoreEntry.SetValue(state);
        }

        public async Task LoadAll()
        {
            var allStateStoreEntries = await StateStore.Load(Namespace);

            foreach (var stateStoreEntry in allStateStoreEntries)
            {
                if (!_state.ContainsKey(stateStoreEntry.Key))
                {
                    _state.Add(stateStoreEntry.Key, new StateEntry(stateStoreEntry));
                }
            }
        }

        public async Task Load(IEnumerable<string> keys)
        {
            var stateStoreEntries = await StateStore.Load(Namespace, keys);

            foreach (var stateStoreEntry in stateStoreEntries)
            {
                if (!_state.ContainsKey(stateStoreEntry.Key))
                {
                    _state.Add(stateStoreEntry.Key, new StateEntry(stateStoreEntry));
                }
            }
        }

        public void Delete(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_state.TryGetValue(key, out var stateEntry))
            {
                stateEntry.IsPendingDeletion = true;
            }
        }

        public async Task SaveChanges()
        {
            var dirtyStateEntries = _state.Values.Where(se => se.IsDirty);

            await StateStore.Save(dirtyStateEntries.Select(se => se.StateStoreEntry));

            foreach (var dirtyStateEntry in dirtyStateEntries)
            {
                dirtyStateEntry.IsDirty = false;
            }

            var stateEntryKeysPendingDeletion = _state.Values.Where(se => se.IsPendingDeletion).Select(se => se.StateStoreEntry.Key).ToList();

            await StateStore.Delete(Namespace, stateEntryKeysPendingDeletion);

            foreach (var key in stateEntryKeysPendingDeletion)
            {
                _state.Remove(key);
            }
        }

        private sealed class StateEntry
        {
            public StateEntry(IStateStoreEntry stateStoreEntry, bool isDirty = false)
            {
                StateStoreEntry = stateStoreEntry;
                IsDirty = isDirty;
            }

            public IStateStoreEntry StateStoreEntry;
            public bool IsDirty;
            public bool IsPendingDeletion;
        }
    }
}
