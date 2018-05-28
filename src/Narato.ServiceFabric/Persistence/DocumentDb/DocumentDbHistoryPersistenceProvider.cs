using Narato.ServiceFabric.Models;
using System;
using System.Threading.Tasks;

namespace Narato.ServiceFabric.Persistence.DocumentDb
{
    public class DocumentDbHistoryPersistenceProvider<T> : DocumentDbPersistenceProvider<T>, IHistoryPersistenceProvider<T> where T : ModelBase, new()
    {
        public DocumentDbHistoryPersistenceProvider(string endPoint, string authKey, string dbName, string collectionName, string accountKey = "")
        : base (endPoint, authKey, dbName, collectionName, accountKey)
        {

        }

        public Task RetrieveHistoryAsync(string key)
        {
            throw new NotImplementedException();
        }
        
    }
}