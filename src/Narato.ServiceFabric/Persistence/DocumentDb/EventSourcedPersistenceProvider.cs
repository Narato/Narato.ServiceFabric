using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonDiffPatch;
using Microsoft.Azure.Documents.Client;
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
            //TODO: in transaction steken
            
            var persistedObject = await RetrieveInternalAsync(model.Key);
            
            if (persistedObject == null)
            {
                SetETag(model);
                
                await _db.CreateDocumentAsync(new PersistedModel<TModel>(model));
            }
            else
            {
                //When doing a patch, the last one wins since we are only updating a part of the object and probably do not have access to the etag.
                //To detect a patch operation, check the e-tag for a null value
                if (persistedObject.Current.ETag != null && persistedObject.Current.ETag != model.ETag)
                    throw new Exception("The object has changed between your read action and this update request. We cannot continue with the save.");
                
                SetETag(model);
                
                persistedObject.Current = model;
                await _db.UpdateDocumentAsync(persistedObject);
            }
            
            if(model.EntityStatus == EntityStatus.Deleted)
                await EventSourcingDeleteRecordAsync(model, true);
            else
                await EventSourcingCreateRecordAsync(persistedObject?.Current, model);
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
            var document = await RetrieveInternalAsync(key);
            await DocDbDatabase.Client.DeleteDocumentAsync(document.Self);
            
            SetETag(document.Current);
            await EventSourcingDeleteRecordAsync(document.Current, false);
        }

        public async Task DeleteAllAsync()
        {
            var queryOptions = new FeedOptions { MaxItemCount = -1 };
            var result = DocDbDatabase.Client
                .CreateDocumentQuery<PersistedModel<TModel>>(UriFactory.CreateDocumentCollectionUri(_db.DatabaseName,
                    _db.CollectionName), queryOptions)
                .Where(c => c.Type == typeof(TModel).Name)
                .AsEnumerable().ToList();
            
            foreach (var timesheet in result)
            {
                await DocDbDatabase.Client.DeleteDocumentAsync(
                    new Uri(_db.EndPoint.Replace(":443", "") + timesheet.Self));
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
        
        private async Task EventSourcingDeleteRecordAsync(TModel modelToDelete, bool isSoftDelete)
        {
            PatchDocument patchDocument;

            if (isSoftDelete)
            {
                var tmpJsonModel =  modelToDelete.ToJson();
                
                modelToDelete.EntityStatus = EntityStatus.Deleted;
                patchDocument = CalculateJsonDiff(tmpJsonModel, modelToDelete.ToJson());
            }
            else
            {
                modelToDelete.EntityStatus = EntityStatus.Deleted;
                patchDocument = CalculateJsonDiff(modelToDelete.ToJson(), "".ToJson());
            }

            await PersistEventSourcingRecordAsync(modelToDelete.Key, patchDocument, modelToDelete);
        }

        private async Task EventSourcingCreateRecordAsync(TModel existingModel, TModel newModel)
        {
            PatchDocument patchDocument;
            
            if (existingModel == null)
            {
                patchDocument = CalculateJsonDiff("".ToJson(), newModel.ToJson());
            }
            else
            {
                patchDocument = CalculateJsonDiff(existingModel.ToJson(), newModel.ToJson());
            }
            
            await PersistEventSourcingRecordAsync(newModel.Key, patchDocument, newModel);
        }
        
        private PatchDocument CalculateJsonDiff(string existingObject, string newObject)
        {
            var existing = JToken.Parse(existingObject);
            var newOne = JToken.Parse(newObject);
            return new JsonDiffer().Diff(existing, newOne, false);
        }
        
        private async Task PersistEventSourcingRecordAsync(string partitionKey, PatchDocument patchDocument, TModel model)
        {
            EventSourcingTableStorageEntity eventSourcingEntity = new EventSourcingTableStorageEntity();
            eventSourcingEntity.Operations = patchDocument.ToString();
            eventSourcingEntity.PartitionKey = partitionKey.Replace("/", ""); //Warning: Some characters are not allowed in the paritionkey
            eventSourcingEntity.Json = model.ToJson();
            eventSourcingEntity.ETag = model.ETag;

            await TableStorage.PersistAsync(eventSourcingEntity);
        }
        
        private void SetETag(TModel modelToUpdate)
        {
            //Create the hash with an empty ETag so the eTag doesn't interfere with the hash result.
            modelToUpdate.ETag = "";
            modelToUpdate.ETag = HashHelper.CreateMD5(modelToUpdate.ToJson());
        }


    }
}