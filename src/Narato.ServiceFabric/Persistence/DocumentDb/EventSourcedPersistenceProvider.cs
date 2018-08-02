using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JsonDiffPatch;
using Microsoft.Azure.Documents.Client;
using Microsoft.WindowsAzure.Storage.Table;
using Narato.ServiceFabric.Helpers;
using Narato.ServiceFabric.Models;
using Narato.StringExtensions;
using Newtonsoft.Json.Linq;

namespace Narato.ServiceFabric.Persistence.DocumentDb
{
    public class EventSourcedPersistenceProvider<TModel> : IPersistenceProvider<TModel>, IEventSourcedPersistenceProvider<TModel>, IHistoryProvider where TModel : ModelBase, new()
    {
        protected readonly TableStorage.TableStorage _tableStorage;
        protected readonly DocDbDatabase _db;
        private string _accountKey;
        private readonly SemaphoreSlim _persistMutex = new SemaphoreSlim(1,1);
        private readonly SemaphoreSlim _deleteMutex = new SemaphoreSlim(1,1);
        private readonly SemaphoreSlim _deleteAllMutex = new SemaphoreSlim(1,1);
        
        public EventSourcedPersistenceProvider(string endPoint, string authKey, string dbName, string collectionName, string cloudStorageConnectionString, string tableName, string accountKey = "")
        {
            _db = new DocDbDatabase(endPoint, authKey, dbName, collectionName);
            _tableStorage = new TableStorage.TableStorage(cloudStorageConnectionString, tableName);
        }

        public async Task PersistAsync(TModel model)
        {
            await PersistAndReturnEntityAsync(model);

        }

        public async Task<EventSourcingTableStorageEntity> PersistAndReturnEntityAsync(TModel model)
        {
            //waits untill the previous task has been finished (sort of like a lock but no 100% the same)
            await _persistMutex.WaitAsync().ConfigureAwait(false);

            EventSourcingTableStorageEntity returnEntity = new EventSourcingTableStorageEntity();

            try
            {
                var persistedObject = RetrieveInternal(model.Key);

                if (persistedObject == null)
                {
                    SetETag(model);

                    await _db.CreateDocumentAsync(new PersistedModel<TModel>(model));

                    //Try catch to handle transaction logic
                    try
                    {
                        returnEntity = await EventSourcingCreateRecordAsync<EventSourcingTableStorageEntity>(null, model);
                    }
                    catch
                    {
                        //Revert the creation of the documentDb doc
                        await DeleteAsync(model.Key, false);
                        throw;
                    }
                }
                else
                {
                    //When doing a patch, the last one wins since we are only updating a part of the object and probably do not have access to the etag.
                    //To detect a patch operation, check the e-tag for a null value
                    if (persistedObject.Current.ETag != null && persistedObject.Current.ETag != model.ETag)
                        throw new Exception("The object has changed between your read action and this update request. We cannot continue with the save.");

                    SetETag(model);

                    //It's important that this happens before the persistedObject.Current is set to the updated model to get the diff...
                    returnEntity = await EventSourcingCreateRecordAsync<EventSourcingTableStorageEntity>(persistedObject.Current, model);

                    try
                    {
                        persistedObject.Current = model;
                        await _db.UpdateDocumentAsync(persistedObject);
                    }
                    catch
                    {
                        //Revert the creation of the event sourcing record
                        //var entityToDelete = await TableStorage.GetSingleEntity<ITableEntity>(newEventSourcingRecord.PartitionKey, newEventSourcingRecord.RowKey);
                        await _tableStorage.DeleteAsync(returnEntity);
                        throw;
                    }
                }
            }
            finally
            {
                _persistMutex.Release();
            }

            return returnEntity;

        }

        public async Task<TModel> RetrieveAsync(string key)
        {
            var result = RetrieveInternal(key);
            return result?.Current;
        }

        public async Task<IEnumerable<TModel>> RetrieveAllAsync()
        {
            var queryOptions = new FeedOptions { MaxItemCount = -1 };

            var result = DocDbDatabase.Client
                .CreateDocumentQuery<PersistedModel<TModel>>(UriFactory.CreateDocumentCollectionUri(_db.DatabaseName,
                    _db.CollectionName), queryOptions)
                .Where(c => c.Type == typeof(TModel).Name)
                .Select(d => d.Current)
                .AsEnumerable();

            return result;
        }

        private PersistedModel<TModel> RetrieveInternal(string key)
        {
            var queryOptions = new FeedOptions { MaxItemCount = -1 };
            var result = DocDbDatabase.Client
                .CreateDocumentQuery<PersistedModel<TModel>>(UriFactory.CreateDocumentCollectionUri(_db.DatabaseName,
                    _db.CollectionName), queryOptions)
                .Where(c => c.Key == key && c.Type == typeof(TModel).Name).ToList()
                .AsEnumerable()
                .FirstOrDefault();

            return result;
        }

        public async Task DeleteAsync(string key)
        {
            await DeleteAsync(key, true);
        }
        
        private async Task DeleteAsync(string key, bool withEventSourcing = true)
        {
            await _deleteMutex.WaitAsync().ConfigureAwait(false);

            try
            {
                var document = RetrieveInternal(key);
                await DocDbDatabase.Client.DeleteDocumentAsync(document.Self);

                if (withEventSourcing)
                {
                    SetETag(document.Current);
                    await EventSourcingDeleteRecordAsync(document.Current);
                }
            }
            finally
            {
                _deleteMutex.Release();
            }

            
        }

        public async Task DeleteAllAsync()
        {
            await _deleteAllMutex.WaitAsync().ConfigureAwait(false);

            try
            {
                var queryOptions = new FeedOptions { MaxItemCount = -1 };
                var results = DocDbDatabase.Client
                    .CreateDocumentQuery<PersistedModel<TModel>>(UriFactory.CreateDocumentCollectionUri(_db.DatabaseName,
                        _db.CollectionName), queryOptions)
                    .Where(c => c.Type == typeof(TModel).Name)
                    .AsEnumerable().ToList();

                foreach (var result in results.ToList())
                {
                    await DocDbDatabase.Client.DeleteDocumentAsync(
                        new Uri(_db.EndPoint.Replace(":443", "") + result.Self));

                    await EventSourcingDeleteRecordAsync(result.Current);
                }
            }
            finally
            {
                _deleteAllMutex.Release();
            }
        }
        
        public async Task<IEnumerable<EventSourcingTableStorageEntity>> RetrieveHistoryAsync(string key)
        {
            return await _tableStorage.GetAllEntityHistory<EventSourcingTableStorageEntity>(key);
        }

        public async Task<IEnumerable<EventSourcingTableStorageEntity>> RetrieveHistoryBeforeDateAsync(string partitionKey, DateTime date)
        {
            return await _tableStorage.GetEntityHistoryBeforeDate<EventSourcingTableStorageEntity>(partitionKey, date);
        }

        public async Task<EventSourcingTableStorageEntity> RetrieveLastEntryBeforeDateAsync(string partitionKey, DateTime date)
        {
            return await _tableStorage.GetLastEntityBeforeDate<EventSourcingTableStorageEntity>(partitionKey, date);
        }

        public async Task<EventSourcingTableStorageEntity> RetrieveEntryByKeyAndRowkeyAsync(string partitionKey, string rowkey)
        {
            return await _tableStorage.GetSingleEntity<EventSourcingTableStorageEntity>(partitionKey, rowkey);
        }

        private async Task EventSourcingDeleteRecordAsync(TModel modelToDelete)
        {
            modelToDelete.EntityStatus = EntityStatus.Deleted;
            var patchDocument = CalculateJsonDiff(modelToDelete.ToJson(), "".ToJson());

            await PersistEventSourcingRecordAsync<ITableEntity>(modelToDelete.Key, patchDocument, modelToDelete);
        }

        private async Task<T> EventSourcingCreateRecordAsync<T>(TModel existingModel, TModel newModel) where T : ITableEntity
        {
            if (newModel.Key == null)
                throw new NullReferenceException("Key must be filled out");
            
            PatchDocument patchDocument;
            
            if (existingModel == null)
            {
                patchDocument = CalculateJsonDiff("".ToJson(), newModel.ToJson());
            }
            else
            {
                patchDocument = CalculateJsonDiff(existingModel.ToJson(), newModel.ToJson());
            }
            
            return await PersistEventSourcingRecordAsync<T>(newModel.Key, patchDocument, newModel);
        }
        
        private PatchDocument CalculateJsonDiff(string existingObject, string newObject)
        {
            var existing = JToken.Parse(existingObject);
            var newOne = JToken.Parse(newObject);
            return new JsonDiffer().Diff(existing, newOne, false);
        }
        
        private async Task<T> PersistEventSourcingRecordAsync<T>(string partitionKey, PatchDocument patchDocument, TModel model) where T : ITableEntity
        {
            EventSourcingTableStorageEntity eventSourcingEntity = new EventSourcingTableStorageEntity();
            eventSourcingEntity.Operations = patchDocument.ToString();
            eventSourcingEntity.PartitionKey = partitionKey.Replace("/", ""); //Warning: Some characters are not allowed in the paritionkey
            eventSourcingEntity.Json = model.ToJson();
            eventSourcingEntity.ETag = model.ETag;

            var newEntity = await _tableStorage.PersistAsync<T>(eventSourcingEntity);
            return newEntity;
        }
        
        private void SetETag(TModel modelToUpdate)
        {
            //Create the hash with an empty ETag so the eTag doesn't interfere with the hash result.
            modelToUpdate.ETag = "";
            modelToUpdate.ETag = HashHelper.CreateMD5(modelToUpdate.ToJson());
        }


    }
}