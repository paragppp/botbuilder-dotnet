using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.State
{
    public interface IStateStore
    {
        IStateStoreEntry CreateNewStateEntry(string partitionKey, string key);

        /// <summary>
        /// Loads all state entries under the specified <paramref name="partitionKey">partition</paramref>.
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        Task<IEnumerable<IStateStoreEntry>> Load(string partitionKey);

        /// <summary>
        /// Loads a single piece of state, identified by its <paramref name="key"/> from the given <paramref name="partitionKey">partition</paramref>.
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<IStateStoreEntry> Load(string partitionKey, string key);

        Task<IEnumerable<IStateStoreEntry>> Load(string partitionKey, IEnumerable<string> keys);

        Task Save(IEnumerable<IStateStoreEntry> values);

        Task Delete(string partitionKey);

        Task Delete(string partitionKey, string key);

        Task Delete(string partitionKey, IEnumerable<string> keys);
    }
}