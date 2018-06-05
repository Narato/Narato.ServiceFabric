using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Narato.ServiceFabric.Persistence.TableStorage
{
    public class TableStoragePersistenceProvider<T> : IHistoryPersistenceProvider<T> where T : TableEntity, new()
    {
        protected readonly TableStorage TableStorage;

        public TableStoragePersistenceProvider(string cloudStorageConnectionString, string tableName)
        {
            TableStorage = new TableStorage(cloudStorageConnectionString, tableName);
        }

        public async Task PersistAsync(TableEntity tableEntity)
        {
            await TableStorage.PersistAsync(tableEntity);
        }

        public async Task<T> RetrieveAsync(string partitionKey, string rowKey)
        {
            var result = await TableStorage.GetSingleEntity<T>(partitionKey, rowKey);
            return result;
        }

        public async Task<IEnumerable<T>> RetrieveHistoryAsync(string key)
        {
            var history = await TableStorage.GetAllEntityHistory<T>(key);

            return history;
        }

        public async Task<IEnumerable<T>> RetrieveHistoryBeforeDateAsync(string partitionKey, DateTime date)
        {
            var history = await TableStorage.GetEntityHistoryBeforeDate<T>(partitionKey, date);

            return history;
        }
    }
}