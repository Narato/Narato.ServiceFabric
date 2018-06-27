using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonDiffPatch;
using Microsoft.Azure.Documents.Client;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.WindowsAzure.Storage.Table;
using Narato.ServiceFabric.Helpers;
using Narato.ServiceFabric.Models;
using Narato.StringExtensions;
using Newtonsoft.Json.Linq;

namespace Narato.ServiceFabric.Persistence.DocumentDb
{
    public class EventSourcedPersistenceProvider<TModel> : IPersistenceProvider<TModel>, IHistoryProvider where TModel : ModelBase, new()
    {
        private readonly TableStorage.TableStorage TableStorage;
        private readonly DocDbDatabase _db;
        private string _accountKey;
        
        public EventSourcedPersistenceProvider(string endPoint, string authKey, string dbName, string collectionName, string cloudStorageConnectionString, string tableName, string accountKey = "")
        {
            _db = new DocDbDatabase(endPoint, authKey, dbName, collectionName);
            TableStorage = new TableStorage.TableStorage(cloudStorageConnectionString, tableName);
        }

        public async Task PersistAsync(TModel model)
        {
            var persistedObject = await RetrieveInternalAsync(model.Key);
            
            if (persistedObject == null)
            {
                SetETag(model);

                await _db.CreateDocumentAsync(new PersistedModel<TModel>(model));

                //Try catch to handle transaction logic
                try
                {
                    await EventSourcingCreateRecordAsync(null, model);
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
                var newEventSourcingRecord = await EventSourcingCreateRecordAsync(persistedObject?.Current, model);

                try
                {
                    persistedObject.Current = model;
                    await _db.UpdateDocumentAsync(persistedObject);
                }
                catch
                {
                    //Revert the creation of the event sourcing record
                    //var entityToDelete = await TableStorage.GetSingleEntity<ITableEntity>(newEventSourcingRecord.PartitionKey, newEventSourcingRecord.RowKey);
                    await TableStorage.DeleteAsync(newEventSourcingRecord);
                    throw;
                }
            }   
        }

        public async Task<TModel> RetrieveAsync(string key)
        {
            var result = await RetrieveInternalAsync(key);
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

        private async Task<PersistedModel<TModel>> RetrieveInternalAsync(string key)
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
            var document = await RetrieveInternalAsync(key);
            await DocDbDatabase.Client.DeleteDocumentAsync(document.Self);

            if (withEventSourcing)
            {
                SetETag(document.Current);
                await EventSourcingDeleteRecordAsync(document.Current);
            }
        }

        public async Task DeleteAllAsync()
        {
            var queryOptions = new FeedOptions { MaxItemCount = -1 };
            var results= DocDbDatabase.Client
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
        
        public async Task<IEnumerable<EventSourcingTableStorageEntity>> RetrieveHistoryAsync(string key)
        {
            return await TableStorage.GetAllEntityHistory<EventSourcingTableStorageEntity>(key);
        }

        public async Task<IEnumerable<EventSourcingTableStorageEntity>> RetrieveHistoryBeforeDateAsync(string partitionKey, DateTime date)
        {
            return await TableStorage.GetEntityHistoryBeforeDate<EventSourcingTableStorageEntity>(partitionKey, date);
        }

        private async Task EventSourcingDeleteRecordAsync(TModel modelToDelete)
        {
            modelToDelete.EntityStatus = EntityStatus.Deleted;
            var patchDocument = CalculateJsonDiff(modelToDelete.ToJson(), "".ToJson());

            await PersistEventSourcingRecordAsync(modelToDelete.Key, patchDocument, modelToDelete);
        }

        private async Task<ITableEntity> EventSourcingCreateRecordAsync(TModel existingModel, TModel newModel)
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
            
            return await PersistEventSourcingRecordAsync(newModel.Key, patchDocument, newModel);
        }
        
        private PatchDocument CalculateJsonDiff(string existingObject, string newObject)
        {
            var existing = JToken.Parse(existingObject);
            var newOne = JToken.Parse(newObject);
            return new JsonDiffer().Diff(existing, newOne, false);
        }
        
        private async Task<ITableEntity> PersistEventSourcingRecordAsync(string partitionKey, PatchDocument patchDocument, TModel model)
        {
            EventSourcingTableStorageEntity eventSourcingEntity = new EventSourcingTableStorageEntity();
            eventSourcingEntity.Operations = patchDocument.ToString();
            eventSourcingEntity.PartitionKey = partitionKey.Replace("/", ""); //Warning: Some characters are not allowed in the paritionkey
            eventSourcingEntity.Json = model.ToJson();
            eventSourcingEntity.ETag = model.ETag;

            var newEntity = await TableStorage.PersistAsync(eventSourcingEntity);
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