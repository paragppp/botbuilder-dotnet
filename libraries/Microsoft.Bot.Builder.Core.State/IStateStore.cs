using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.State
{
    public interface IStateStore
    {
        IStateStoreEntry CreateNewStateEntry(string stateNamespace, string key);

        /// <summary>
        /// Loads all state entries under the specified <paramref name="stateNamespace">partition</paramref>.
        /// </summary>
        /// <param name="stateNamespace"></param>
        /// <returns></returns>
        Task<IEnumerable<IStateStoreEntry>> Load(string stateNamespace);

        /// <summary>
        /// Loads a single piece of state, identified by its <paramref name="key"/> from the given <paramref name="stateNamespace">partition</paramref>.
        /// </summary>
        /// <param name="stateNamespace"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<IStateStoreEntry> Load(string stateNamespace, string key);

        Task<IEnumerable<IStateStoreEntry>> Load(string stateNamespace, IEnumerable<string> keys);

        Task Save(IEnumerable<IStateStoreEntry> stateStoreEntries);

        Task Delete(string stateNamespace);

        Task Delete(string stateNamespace, IEnumerable<string> keys);
    }

    public static class StateStoreExtensions
    {
        public static Task Save(this IStateStore stateStore, params IStateStoreEntry[] stateStoreEntries) => stateStore.Save((IEnumerable<IStateStoreEntry>)stateStoreEntries);

        public static Task Delete(this IStateStore stateStore, string stateNamespace, params string[] keys) => stateStore.Delete(stateNamespace, (IEnumerable<string>)keys);
    }
}