using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Narato.ServiceFabric.Persistence.DocumentDb
{
    public class DocDbDatabase
    {
        public string EndPoint { get; }
        public string AuthKey { get; }
        public string DatabaseName { get; }
        public string CollectionName { get; }
        private static DocumentClient _client;

        public static DocumentClient Client => _client;

        public DocDbDatabase(string endPoint, string authKey, string databaseName, string collectionName)
        {
            EndPoint = endPoint;
            AuthKey = authKey;
            DatabaseName = databaseName;
            CollectionName = collectionName;

            if (_client == null)
            {
                Uri endpointUri = new Uri(EndPoint);
                _client = new DocumentClient(endpointUri, AuthKey);
            }


            //  Initialize();
        }


        private async void Initialize()
        {
            await CreateDatabaseIfNotExists(DatabaseName);
            await CreateDocumentCollectionIfNotExists(DatabaseName, CollectionName);
        }

        private async Task CreateDatabaseIfNotExists(string databaseName)
        {
            // Check to verify a database with the id=FamilyDB does not exist
            try
            {
                await Client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(databaseName));
            }
            catch (DocumentClientException de)
            {
                // If the database does not exist, create a new database
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await Client.CreateDatabaseAsync(new Database { Id = databaseName });
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task CreateDocumentCollectionIfNotExists(string databaseName, string collectionName)
        {
            try
            {
                await Client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName));
            }
            catch (DocumentClientException de)
            {
                // If the document collection does not exist, create a new collection
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    DocumentCollection collectionInfo = new DocumentCollection
                    {
                        Id = collectionName,
                        IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 })
                    };

                    // Configure collections for maximum query flexibility including string range queries.

                    // Here we create a collection with 400 RU/s.
                    await Client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(databaseName),
                        collectionInfo,
                        new RequestOptions { OfferThroughput = 400 });
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task CreateDocumentAsync(object docToCreate)
        {
            await Client.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName),
                docToCreate);
        }

        public async Task UpdateDocumentAsync(object docToUpdate)
        {
            await Client.UpsertDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName),
                docToUpdate);
        }
    }
}
