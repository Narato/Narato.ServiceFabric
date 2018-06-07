using System;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using JsonDiffPatch;
using Narato.ResponseMiddleware.Models.Exceptions;
using Narato.ServiceFabric.Helpers;
using Narato.ServiceFabric.Models;
using Narato.ServiceFabric.Persistence;
using Narato.StringExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Narato.ServiceFabric.Services.EventSourcing
{
    public abstract class StatelessCrudServiceEventSourcingBase<TModel> : StatelessCrudServiceBase<TModel>
        where TModel : ModelBase, new()
    {
        private readonly IHistoryPersistenceProvider<EventSourcingTableStorageEntity> _historyPersistenceProvider;
        private readonly IPersistenceProvider<TModel> _dataPersistenceProvider;
        private readonly bool _softDeleteEnabled;

        protected StatelessCrudServiceEventSourcingBase(StatelessServiceContext serviceContext, IHistoryPersistenceProvider<EventSourcingTableStorageEntity> historyPersistenceProvider, IPersistenceProvider<TModel> dataPersistenceProvider, bool softDeleteEnabled) 
            : base(serviceContext, dataPersistenceProvider, softDeleteEnabled)
        {
            _historyPersistenceProvider = historyPersistenceProvider;
            _dataPersistenceProvider = dataPersistenceProvider;
            _softDeleteEnabled = softDeleteEnabled;
        }

        public new virtual async Task<TModel> CreateAsync(TModel modelToCreate)
        {
            SetETag(modelToCreate);
            var newModel = await base.CreateAsync(modelToCreate);

            if (newModel != null)
            {
                await EventSourcingCreateRecordAsync(newModel);
            }

            return newModel;
        }

        public new virtual async Task<TModel> UpdateAsync(TModel modelToUpdate)
        {
            //Get the latest entity from the table storage.
            var existingEntity = await GetLatestEntityVersionAsync(modelToUpdate.Key);

            //When doing a patch, the last one wins since we are only updating a part of the object and probably do not have access to the etag.
            //To detect a patch operation, check the e-tag for a null value
            if (existingEntity.ETag != null && existingEntity.ETag != modelToUpdate.ETag)
                throw new Exception("Someone has already updated the object you are trying to save. We cannot continue with the save");

            SetETag(modelToUpdate);
            var updatedModel = await base.UpdateAsync(modelToUpdate);

            await EventSourcingUpdateRecordAsync(existingEntity, modelToUpdate);

            return updatedModel;
        }

        public new virtual async Task DeleteAsync(string key)
        {
            var existingEntity = await GetAsync(key);

            if (_softDeleteEnabled)
            {
                existingEntity.EntityStatus = EntityStatus.Deleted;
                existingEntity.StatusChangedAt = DateTime.UtcNow;
                SetETag(existingEntity);

                await _dataPersistenceProvider.PersistAsync(existingEntity);
            }
            else
            {
                await _dataPersistenceProvider.DeleteAsync(key);
            }

            await EventSourcingDeleteRecordAsync(existingEntity, _softDeleteEnabled);
        }        

        /// <summary>
        /// Returns the entity from the eventSource DB that matches the specific date or the one closest to (before) it
        /// </summary>
        public virtual async Task<TModel> GetByDateAsync(string key, DateTime date)
        {
            var existingTableEntity = await _historyPersistenceProvider.RetrieveHistoryBeforeDateAsync(key, date);
            var entity = existingTableEntity.FirstOrDefault();
            
            if (entity == null)
                throw new EntityNotFoundException("ENF", $"Entity with key '{key}' was not found with a timestamp on or before the given datetime '{date}'.");
            
            var existingEntity = JsonConvert.DeserializeObject<TModel>(entity.Json);
            
            return existingEntity;
        }
        
        //Gets the entity from the table storage
        private async Task<TModel> GetLatestEntityVersionAsync(string key)
        {
            return await GetByDateAsync(key, DateTime.Now);
        }

        private PatchDocument CalculateJsonDiff(string existingObject, string newObject)
        {
            var existing = JToken.Parse(existingObject);
            var newOne = JToken.Parse(newObject);
            return new JsonDiffer().Diff(existing, newOne, false);
        }

        private async Task EventSourcingCreateRecordAsync(TModel newModel)
        {
            var patchDocument = CalculateJsonDiff("".ToJson(), newModel.ToJson());
            await PersistEventSourcingRecordAsync(newModel.Key, patchDocument, newModel);
        }

        private async Task EventSourcingUpdateRecordAsync(TModel existingModel, TModel newModel)
        {
            PatchDocument patchDocument = CalculateJsonDiff(existingModel.ToJson(), newModel.ToJson());
            await PersistEventSourcingRecordAsync(newModel.Key, patchDocument, newModel);
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

        private async Task PersistEventSourcingRecordAsync(string partitionKey, PatchDocument patchDocument, TModel model)
        {
            EventSourcingTableStorageEntity eventSourcingEntity = new EventSourcingTableStorageEntity();
            eventSourcingEntity.Operations = patchDocument.ToString();
            eventSourcingEntity.PartitionKey = partitionKey.Replace("/", ""); //Warning: Some characters are not allowed in the paritionkey
            eventSourcingEntity.Json = model.ToJson();
            eventSourcingEntity.ETag = model.ETag;

            await _historyPersistenceProvider.PersistAsync(eventSourcingEntity);
        }

        private void SetETag(TModel modelToUpdate)
        {
            //Create the hash with an empty ETag so the eTag doesn't interfere with the hash result.
            modelToUpdate.ETag = "";
            modelToUpdate.ETag = HashHelper.CreateMD5(modelToUpdate.ToJson());
        }
    }
}