using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Narato.ServiceFabric.Models;

namespace Narato.ServiceFabric.Persistence.TableStorage
{
    public class TableStoragePersistenceProvider<T> : IHistoryProvider<T>, IPersistenceProvider<T> where T : TableStorageModelBase, new()
    {
        protected readonly TableStorage _tableStorage;

        public TableStoragePersistenceProvider(string cloudStorageConnectionString, string tableName)
        {
            _tableStorage = new TableStorage(cloudStorageConnectionString, tableName);
        }

        public async Task PersistAsync(T model)
        {
            await _tableStorage.PersistAsync(model);
        }

        public async Task<T> RetrieveAsync(string partitionKey, string rowKey)
        {
            var result = await _tableStorage.GetSingleEntity<T>(partitionKey, rowKey);
            return result;
        }

        public Task RetrieveHistoryAsync(string key)
        {
            throw new NotImplementedException();
        }


        //Following will never be implemented for the event sourcing provider TODO: create new interface?
        public Task DeleteAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> RetrieveAllAsync()
        {
            throw new NotImplementedException();
        }


    }
}
