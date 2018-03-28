﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.State
{
    public class FileSystemStateStore : IStateStore
    {
        private static readonly JsonSerializer StateStoreEntrySerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            // TODO: dial in all settings
        });

        private readonly string _basePath;

        public FileSystemStateStore(string basePath)
        {
            _basePath = basePath;
        }

        public IStateStoreEntry CreateNewStateEntry(string stateNamespace, string key) => new FileSystemStateStoreEntry(stateNamespace, key, GetStateStoreEntryFilePath(_basePath, stateNamespace, key));

        public Task Delete(string stateNamespace)
        {
            Directory.Delete(ConvertStateNamespaceToDirectoryPath(_basePath, stateNamespace));

            return Task.CompletedTask;
        }

        public Task Delete(string stateNamespace, IEnumerable<string> keys)
        {
            var stateNamespaceDirectoryInfo = GetStateNamespaceDirectoryInfo(stateNamespace);

            if (stateNamespaceDirectoryInfo.Exists)
            {
                var distinctKeys = new HashSet<string>();

                foreach (var key in keys)
                {
                    distinctKeys.Add(ConvertKeyToFileName(key));
                }

                foreach (var stateEntryFile in stateNamespaceDirectoryInfo.EnumerateFiles())
                {
                    if (distinctKeys.Contains(stateEntryFile.Name))
                    {
                        stateEntryFile.Delete();
                    }
                }
            }

            return Task.CompletedTask;
        }

        public async Task<IEnumerable<IStateStoreEntry>> Load(string stateNamespace)
        {
            var stateNamespaceDirectoryInfo = GetStateNamespaceDirectoryInfo(stateNamespace);

            if (!stateNamespaceDirectoryInfo.Exists)
            {
                return Enumerable.Empty<IStateStoreEntry>();
            }

            var loadTasks = new List<Task<IStateStoreEntry>>();

            foreach (var stateEntryFile in stateNamespaceDirectoryInfo.EnumerateFiles())
            {
                var stateEntryKey = Path.GetFileNameWithoutExtension(stateEntryFile.Name);

                loadTasks.Add(Task.Run(() => LoadFileSystemStateStoreEntry(stateNamespace, stateEntryKey, new FileInfo(stateEntryKey))));
            }

            await Task.WhenAll(loadTasks);

            var results = new List<IStateStoreEntry>(loadTasks.Count);

            foreach (var loadTask in loadTasks)
            {
                results.Add(loadTask.Result);
            }

            return results;
        }

        public Task<IStateStoreEntry> Load(string stateNamespace, string key)
        {
            var storeEntryFileInfo = GetStateStoreEntryFileInfo(_basePath, stateNamespace, key);

            return LoadFileSystemStateStoreEntry(stateNamespace, key, storeEntryFileInfo);
        }

        public async Task<IEnumerable<IStateStoreEntry>> Load(string stateNamespace, IEnumerable<string> keys)
        {
            var loadTasks = new List<Task<IStateStoreEntry>>();

            foreach (var key in keys)
            {
                loadTasks.Add(Load(stateNamespace, key));
            }

            await Task.WhenAll(loadTasks);

            var results = new List<IStateStoreEntry>(loadTasks.Count);

            foreach (var loadTask in loadTasks)
            {
                results.Add(loadTask.Result);
            }

            return results;
        }

        public Task Save(IEnumerable<IStateStoreEntry> stateStoreEntries)
        {
            var saveTasks = new List<Task>();

            foreach (var stateStoreEntry in stateStoreEntries)
            {
                if(!(stateStoreEntry is FileSystemStateStoreEntry fileSystemStateStoreEntry))
                {
                    throw new InvalidOperationException($"Only instances of {nameof(FileSystemStateStoreEntry)} are supported by {nameof(FileSystemStateStore)}.");
                }

                saveTasks.Add(fileSystemStateStoreEntry.WriteToFile());
            }

            return Task.WhenAll(saveTasks);
        }

        private DirectoryInfo GetStateNamespaceDirectoryInfo(string stateNamespace) => new DirectoryInfo(ConvertStateNamespaceToDirectoryPath(_basePath, stateNamespace));

        private static FileInfo GetStateStoreEntryFileInfo(string basePath, string stateNamespace, string key) => new FileInfo(GetStateStoreEntryFilePath(basePath, stateNamespace, key));


        private static string GetStateStoreEntryFilePath(string basePath, string stateNamespace, string key) => Path.Combine(ConvertStateNamespaceToDirectoryPath(basePath, stateNamespace), ConvertKeyToFileName(key));

        private static string ConvertStateNamespaceToDirectoryPath(string basePath, string stateNamespace)
        {
            // TODO: deal with illegal characters

            return Path.Combine(basePath, stateNamespace);
        }

        private static string ConvertKeyToFileName(string key)
        {
            // TODO: deal with illegal characters
            //string normalizedKeyFileName = Path.InvalidPathChars

            return Path.ChangeExtension(key, "json");
        }

        private async Task<IStateStoreEntry> LoadFileSystemStateStoreEntry(string stateNamespace, string key, FileInfo storeEntryFileInfo)
        {
            try
            {
                var memoryStream = new MemoryStream((int)storeEntryFileInfo.Length);

                using (var fileStream = new FileStream(storeEntryFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, options: FileOptions.Asynchronous | FileOptions.SequentialScan))
                using (var textReader = new StreamReader(fileStream, Encoding.UTF8))
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    var data = (JObject)(await JObject.ReadFromAsync(jsonReader));

                    return new FileSystemStateStoreEntry(stateNamespace, key, storeEntryFileInfo.FullName, data);
                }                
            }
            catch (FileNotFoundException)
            {
                return default(IStateStoreEntry);
            }
        }

        private sealed class FileSystemStateStoreEntry : DeferredValueStateStoreEntry
        {
            private JObject _data;

            public FileSystemStateStoreEntry(string stateNamespace, string key, string filePath) : base(stateNamespace, key)
            {
                FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            }

            public FileSystemStateStoreEntry(string stateNamespace, string key, string filePath, JObject data) : this(stateNamespace, key, filePath)
            {
                _data = data ?? throw new ArgumentNullException(nameof(data));

                ETag = data.Value<string>("__BotFramework_ETag");
            }

            public string FilePath { get; }

            public async Task WriteToFile()
            {
tryAgain:
                try
                {
                    using (var fileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, bufferSize: 8192, options: FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        _data = RawValue != null ? JObject.FromObject(RawValue) : new JObject();

                        _data["__BotFramework_RawStateNamespace"] = Namespace;
                        _data["__BotFramework_RawKey"] = Key;
                        _data["__BotFramework_ETag"] = Guid.NewGuid().ToString("N");

                        using (var textWriter = new StreamWriter(fileStream, Encoding.UTF8))
                        using (var jsonWriter = new JsonTextWriter(textWriter))
                        {
                            await _data.WriteToAsync(jsonWriter);
                        }
                    }
                }
                catch(DirectoryNotFoundException)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

                    goto tryAgain;
                }
            }

            protected override T MaterializeValue<T>()
            {
                if(_data == null)
                {
                    return default(T);
                }

                return _data.ToObject<T>();
            }
        }
    }
}
