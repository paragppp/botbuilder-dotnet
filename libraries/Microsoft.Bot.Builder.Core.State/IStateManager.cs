using System.Collections.Generic;
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
}
