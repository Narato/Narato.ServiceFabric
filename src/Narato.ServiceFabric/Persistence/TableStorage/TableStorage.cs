using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Narato.ServiceFabric.Models;

namespace Narato.ServiceFabric.Persistence.TableStorage
{
    public class TableStorage
    {
        public string TableName { get; }
        private readonly CloudTable _eventsTable;

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

        public async Task<T> PersistAsync<T>(TableEntity model) where T : ITableEntity
        {
            //Only do inserts in the event sourcing table
            return await CreateRecordAsync<T>(model);
        }

        public async Task DeleteAsync(ITableEntity tableEntity)
        {
            TableOperation deleteOperation = TableOperation.Delete(tableEntity);
            await _eventsTable.ExecuteAsync(deleteOperation);
        }

        private async Task<T> CreateRecordAsync<T>(TableEntity entityToCreate) where T : ITableEntity
        {
            entityToCreate.RowKey = Guid.NewGuid().ToString();
            TableOperation insertOperation = TableOperation.Insert(entityToCreate);
            var tableResult = await _eventsTable.ExecuteAsync(insertOperation);
            return (T)tableResult.Result;
        }

        //PartitionKey and rowkey form the key
        public async Task<T> GetSingleEntity<T>(string partitionKey, string rowKey) where T : ITableEntity
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            TableResult retrievedResult = await _eventsTable.ExecuteAsync(retrieveOperation);

            return (T)retrievedResult.Result;
        }

        public async Task<IEnumerable<T>> GetAllEntityHistory<T>(string partitionKey) where T : ITableEntity, new ()
        {
            TableContinuationToken token = null;
            string partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            TableQuery<T> query = new TableQuery<T>().Where(TableQuery.CombineFilters(partitionFilter, TableOperators.And, partitionFilter));

            var result = await _eventsTable.ExecuteQuerySegmentedAsync(query, token);

            return result.Results.ToList();
        }

        public async Task<IEnumerable<T>> GetEntityHistoryBeforeDate<T>(string partitionKey, DateTime date) where T : ITableEntity, new()
        {
            string partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            string dateFilter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThanOrEqual, new DateTimeOffset(date.ToUniversalTime()));

            TableContinuationToken token = null;
            TableQuery<T> query = new TableQuery<T>().Where(TableQuery.CombineFilters(partitionFilter, TableOperators.And, dateFilter));

            
            var result = await _eventsTable.ExecuteQuerySegmentedAsync<T>(query, token);
            var tmpResult = result.Results.ToList();
            var returnResult = tmpResult.Cast<T>();

            return returnResult;
        }

        public async Task<T> GetLastEntityBeforeDate<T>(string partitionKey, DateTime date) where T : ITableEntity, new()
        {
            string partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            string dateFilter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThanOrEqual, new DateTimeOffset(date.ToUniversalTime()));

            TableContinuationToken token = null;
            TableQuery<T> query = new TableQuery<T>().Where(TableQuery.CombineFilters(partitionFilter, TableOperators.And, dateFilter));

            var result = await _eventsTable.ExecuteQuerySegmentedAsync<T>(query, token);
            var tmpResult = result.Results.ToList();

            // Todo: move this filtering to the query, but this is not jet available as .NetStandard doesn't implement the required linq stuff
            var lastEntry = tmpResult.OrderBy(r => r.Timestamp).FirstOrDefault();

            return lastEntry;
        }
    }
}