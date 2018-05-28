using System.Threading.Tasks;
using Microsoft.Azure.Amqp.Serialization;
using Microsoft.Azure.Documents.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Narato.ServiceFabric.Persistence.TableStorage
{
    public class TableStorage
    {
        public string TableName { get; }
        private CloudTable _eventsTable;
        
        public TableStorage(string cloudStorageConnectionString, string tableName)
        {
            TableName = tableName;

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(cloudStorageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            _eventsTable = tableClient.GetTableReference(TableName);
            
            //  Initialize();
        }


        private async void Initialize()
        {
            await _eventsTable.CreateIfNotExistsAsync();
        }

        public async Task PersistAsync(TableEntity model)
        {
            var persistedObject = await GetSingleEntity(model.PartitionKey, model.RowKey);

            if (persistedObject?.Result == null)
            {
                await CreateRecordAsync(model);
            }
            else
            {
                var operation = TableOperation.Replace(model);
                await _eventsTable.ExecuteAsync(operation);
            }
        }

        private async Task CreateRecordAsync(TableEntity entityToCreate)
        {
            TableOperation insertOperation = TableOperation.Insert(entityToCreate);
            await _eventsTable.ExecuteAsync(insertOperation);
        }

        public async Task<T> GetSingleEntity<T>(string partitionKey, string rowKey) where T : ITableEntity
        {
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            TableResult retrievedResult = await _eventsTable.ExecuteAsync(retrieveOperation);

            return (T)retrievedResult.Result;
        }
    }
}
