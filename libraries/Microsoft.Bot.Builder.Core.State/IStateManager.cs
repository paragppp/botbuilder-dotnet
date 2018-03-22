using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.State
{
    public interface IStateManager
    {
        Task<TState> Get<TState>(string key) where TState : class;

        void Set<TState>(string key, TState state) where TState : class;

        Task Load();

        Task Load(IEnumerable<string> keys);

        Task SaveChanges();
    }

    public static class StateManagerExtensions
    {
        public static Task<TState> Get<TState>(this IStateManager stateManager) where TState : class =>
            stateManager.Get<TState>(typeof(TState).Name);

        public static void Set<TState>(this IStateManager stateManager, TState state) where TState : class =>
            stateManager.Set<TState>(typeof(TState).Name, state);

    }

    public interface IConversationStateManager : IStateManager
    {
    }

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

        public async Task<TState> Get<TState>(string key) where TState : class
        {
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

            return stateEntry.StateStoreEntry.GetValue<TState>();
        }

        public void Set<TState>(string key, TState state) where TState : class
        {
            if (!_state.TryGetValue(key, out var stateEntry))
            {
                var stateStoreEntry = StateStore.CreateNewStateEntry(Namespace, key);

                _state.Add(key, new StateEntry(stateStoreEntry, isDirty: true));
            }
            else
            {
                stateEntry.IsDirty = true;
            }

            stateEntry.StateStoreEntry.SetValue(state);
        }

        public async Task Load()
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

        public async Task SaveChanges()
        {
            var dirtyStateEntries = _state.Values.Where(se => se.IsDirty);

            await StateStore.Save(dirtyStateEntries.Select(se => se.StateStoreEntry));

            foreach (var dirtyStateEntry in dirtyStateEntries)
            {
                dirtyStateEntry.IsDirty = false;
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
        }
    }

    public class ConversationStateManager : StateManager, IConversationStateManager
    {
        public ConversationStateManager(string channelId, string conversationId, IStateStore stateStore) : base($"/channels/{channelId}/conversations/{conversationId}", stateStore)
        {
        }
    }

    public interface IUserStateManager : IStateManager
    {
    }

    public class UserStateManager : StateManager, IUserStateManager
    {
        public UserStateManager(string userId, IStateStore stateStore) : base($"/users/{userId}", stateStore)
        {
        }
    }
}
