using System;
using System.Collections.Generic;
using System.Text;
using Narato.ServiceFabric.Contracts.Models;
using Narato.ServiceFabric.Persistence.DocumentDb;

namespace Narato.ServiceFabric.TestService.Providers
{
    public class DocumentDbEventSourcingTestModelProvider : EventSourcedPersistenceProvider<TestModel>
    {
        public DocumentDbEventSourcingTestModelProvider(string endPoint, string authKey, string dbName, string collectionName, string cloudStorageConnectionString, string tableName, string accountKey = "") : base(endPoint, authKey, dbName, collectionName, cloudStorageConnectionString, tableName, accountKey)
        {
        }
    }
}
