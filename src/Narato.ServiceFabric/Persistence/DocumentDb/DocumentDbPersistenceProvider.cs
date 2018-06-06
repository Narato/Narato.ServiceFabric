using Microsoft.Azure.Documents.Client;
using Narato.ServiceFabric.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Narato.ServiceFabric.Persistence.DocumentDb
{
    public class DocumentDbPersistenceProvider<T> : IPersistenceProvider<T> where T : ModelBase, new()
    {
        protected readonly DocDbDatabase _db;
        private string _accountKey;

        public DocumentDbPersistenceProvider(string endPoint, string authKey, string dbName, string collectionName, string accountKey = "")
        {
            _db = new DocDbDatabase(endPoint, authKey, dbName, collectionName);
            _accountKey = accountKey.Trim().ToLower();
        }

        public async Task PersistAsync(T model)
        {
            var persistedObject = await RetrieveInternalAsync(model.Key);

            if (persistedObject == null)
            {

                await _db.CreateDocumentAsync(new PersistedModel<T>(model));
            }
            else
            {
                persistedObject.Current = model;
                await _db.UpdateDocumentAsync(persistedObject);
            }
        }

        public async Task<T> RetrieveAsync(string key)
        {
            var result = await RetrieveInternalAsync(key);
            return result?.Current;
        }

        public async Task<IEnumerable<T>> RetrieveAllAsync()
        {
            var queryOptions = new FeedOptions { MaxItemCount = -1 };

            var result = DocDbDatabase.Client
                .CreateDocumentQuery<PersistedModel<T>>(UriFactory.CreateDocumentCollectionUri(_db.DatabaseName,
                    _db.CollectionName), queryOptions)
                .Where(c => c.Type == typeof(T).Name)
                .Select(d => d.Current)
                .AsEnumerable();

            return result;
        }

        private async Task<PersistedModel<T>> RetrieveInternalAsync(string key)
        {
            var queryOptions = new FeedOptions { MaxItemCount = -1 };
            var result = DocDbDatabase.Client
                .CreateDocumentQuery<PersistedModel<T>>(UriFactory.CreateDocumentCollectionUri(_db.DatabaseName,
                    _db.CollectionName), queryOptions)
                .Where(c => c.Key == key && c.Type == typeof(T).Name).ToList()
                .AsEnumerable()
                .FirstOrDefault();

            return result;
        }


        public async Task DeleteAsync(string key)
        {
            throw new System.NotImplementedException();
        }

        public async Task DeleteAllAsync()
        {
            var queryOptions = new FeedOptions { MaxItemCount = -1 };
            var result = DocDbDatabase.Client
                .CreateDocumentQuery<PersistedModel<T>>(UriFactory.CreateDocumentCollectionUri(_db.DatabaseName,
                    _db.CollectionName), queryOptions)
                .Where(c => c.Type == typeof(T).Name)
                .AsEnumerable().ToList();
            foreach (var timesheet in result)
            {
                await DocDbDatabase.Client.DeleteDocumentAsync(
                    new Uri(_db.EndPoint.Replace(":443", "") + timesheet.Self));
            }
        }

    }
}