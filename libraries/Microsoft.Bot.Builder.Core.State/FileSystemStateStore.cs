using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.State
{
    public class FileSystemStateStore : IStateStore
    {
        private readonly string _basePath;

        public FileSystemStateStore(string basePath)
        {
            _basePath = basePath;
        }

        public IStateStoreEntry CreateNewStateEntry(string stateNamespace, string key)
        {
            throw new NotImplementedException();
        }

        public Task Delete(string stateNamespace)
        {
            throw new NotImplementedException();
        }

        public Task Delete(string stateNamespace, IEnumerable<string> keys)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IStateStoreEntry>> Load(string stateNamespace)
        {
            throw new NotImplementedException();
        }

        public Task<IStateStoreEntry> Load(string stateNamespace, string key)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IStateStoreEntry>> Load(string stateNamespace, IEnumerable<string> keys)
        {
            throw new NotImplementedException();
        }

        public Task Save(IEnumerable<IStateStoreEntry> stateStoreEntries)
        {
            throw new NotImplementedException();
        }
    }
}
