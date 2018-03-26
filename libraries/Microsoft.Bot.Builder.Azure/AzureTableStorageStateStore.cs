// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Core.State;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Azure
{
    /// <summary>
    /// Models IStorage around a dictionary 
    /// </summary>
    public class AzureTableStorageStateStore : IStateStore
    {
        private static HashSet<string> CheckedTables = new HashSet<string>();

        private readonly CloudTable _table;

        public AzureTableStorageStateStore(string dataConnectionString, string tableName) : this(CloudStorageAccount.Parse(dataConnectionString), tableName)
        {
        }

        public AzureTableStorageStateStore(CloudStorageAccount storageAccount, string tableName)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(tableName);

            if (CheckedTables.Add($"{storageAccount.TableStorageUri.PrimaryUri.Host}-{tableName}"))
            {
                _table.CreateIfNotExistsAsync().Wait();
            }
        }

        public CloudTable Table => _table;

        public IStateStoreEntry CreateNewStateEntry(string stateNamespace, string key) => new AzureTableStorageEntityStateStoreEntry(new DynamicTableEntity(stateNamespace, key));
        
        public async Task<IEnumerable<IStateStoreEntry>> Load(string stateNamespace)
        {
            var queryContinuationToken = default(TableContinuationToken);
            var query = new TableQuery()
                            .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, stateNamespace));


            var results = new List<IStateStoreEntry>();

            do
            {
                var entitySegment = await _table.ExecuteQuerySegmentedAsync(query, queryContinuationToken).ConfigureAwait(false);

                results.AddRange(entitySegment.Results.Select(entity => new AzureTableStorageEntityStateStoreEntry(entity)));

                queryContinuationToken = entitySegment.ContinuationToken;
            } while (queryContinuationToken != default(TableContinuationToken));

            return results;
        }

        public async Task<IStateStoreEntry> Load(string stateNamespace, string key)
        {
            var tableResult = await _table.ExecuteAsync(TableOperation.Retrieve(stateNamespace, key)).ConfigureAwait(false);

            return new AzureTableStorageEntityStateStoreEntry((DynamicTableEntity)tableResult.Result);
        }

        public async Task<IEnumerable<IStateStoreEntry>> Load(string stateNamespace, IEnumerable<string> keys)
        {
            var loadTasks = new List<Task<IStateStoreEntry>>();

            foreach(var key in keys)
            {
                loadTasks.Add(Load(stateNamespace, key));
            }

            await Task.WhenAll(loadTasks);

            var results = new List<IStateStoreEntry>(loadTasks.Count);

            foreach(var loadTask in loadTasks)
            {
                results.Add(loadTask.Result);
            }

            return results;
        }

        public async Task Save(IEnumerable<IStateStoreEntry> stateStoreEntries)
        {
            foreach(var stateNamespaceBatch in stateStoreEntries.GroupBy(stateStoreEntry => stateStoreEntry.Namespace))
            {
                var partitionBatchOperation = new TableBatchOperation();

                foreach (var stateStoreEntry in stateNamespaceBatch)
                {
                    if (!(stateStoreEntry is AzureTableStorageEntityStateStoreEntry azureTableStorageStateStoreEntry))
                    {
                        throw new InvalidOperationException($"{nameof(AzureTableStorageStateStore)} only supports instances of type {nameof(AzureTableStorageEntityStateStoreEntry)}.");
                    }

                    var value = azureTableStorageStateStoreEntry.RawValue;
                    var tableEntity = azureTableStorageStateStoreEntry.TableEntity;

                    var operation = value == null ? TableOperation.Delete(tableEntity) : TableOperation.InsertOrMerge(tableEntity);

                    partitionBatchOperation.Add(operation);

                    if (partitionBatchOperation.Count == 100)
                    {
                        await _table.ExecuteBatchAsync(partitionBatchOperation);

                        partitionBatchOperation.Clear();
                    }
                }

                if(partitionBatchOperation.Count > 0)
                {
                    await _table.ExecuteBatchAsync(partitionBatchOperation);
                }
            }
        }

        public Task Delete(string stateNamespace)
        {
            throw new NotImplementedException();
        }

        public Task Delete(string stateNamespace, string key)
        {
            throw new NotImplementedException();
        }

        public Task Delete(string stateNamespace, IEnumerable<string> keys)
        {
            throw new NotImplementedException();
        }

        private sealed class AzureTableStorageEntityStateStoreEntry : DeferredValueStateStoreEntry
        {
            private readonly DynamicTableEntity _tableEntity;

            public AzureTableStorageEntityStateStoreEntry(DynamicTableEntity tableEntity) : base(tableEntity.PartitionKey, tableEntity.RowKey, tableEntity.ETag)
            {
                _tableEntity = tableEntity ?? throw new ArgumentNullException(nameof(tableEntity));
            }

            internal DynamicTableEntity TableEntity => _tableEntity;

            protected override T MaterializeValue<T>() => EntityPropertyConverter.ConvertBack<T>(_tableEntity.Properties, null);
        }
    }
}
