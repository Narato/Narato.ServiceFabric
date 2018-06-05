using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonDiffPatch;
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
        private readonly bool _softDeleteEnabled;

        protected StatelessCrudServiceEventSourcingBase(StatelessServiceContext serviceContext, IHistoryPersistenceProvider<EventSourcingTableStorageEntity> historyPersistenceProvider, IPersistenceProvider<TModel> dataPersistenceProvider, bool softDeleteEnabled) 
            : base(serviceContext, dataPersistenceProvider, softDeleteEnabled)
        {
            _historyPersistenceProvider = historyPersistenceProvider;
            _softDeleteEnabled = softDeleteEnabled;
        }

        public new virtual async Task<TModel> Create(TModel modelToCreate)
        {
            TModel newModel = modelToCreate;

            SetETag(modelToCreate);
            newModel = await base.Create(modelToCreate);

            if (newModel != null)
            {
                await EventSourcingCreateRecord(newModel);
            }

            return newModel;
        }

        public new virtual async Task<TModel> Update(TModel modelToUpdate)
        {
            //Get the latest entity from the table storage.
            var existingEntity = GetLatestEntityVersion(modelToUpdate.Key);

            //When doing a patch, the last one wins since we are only updating a part of the object and probably do not have access to the etag
            //to detect a patch, check the e-tag for a null value
            if (existingEntity.ETag != null && existingEntity.ETag != modelToUpdate.ETag)
                throw new Exception("Someone has already updated the object you are trying to save. We cannot continue with the save");

            SetETag(modelToUpdate);
            var updatedModel = await base.Update(modelToUpdate);

            await EventSourcingUpdateRecord(existingEntity, modelToUpdate);

            return updatedModel;
        }

        public new virtual async Task Delete(string key)
        {
            var existingEntity = await base.Get(key);

            await base.Delete(key);
            existingEntity.EntityStatus = EntityStatus.Deleted;
            await EventSourcingDeleteRecord(existingEntity, _softDeleteEnabled);
        }

        public new virtual async Task<TModel> Get(string key)
        {
            return await base.Get(key);
        }

        public new virtual async Task<List<TModel>> GetAll()
        {
            return await base.GetAll();
        }

        private TModel GetLatestEntityVersion(string key)
        {
            var existingTableEntity = _historyPersistenceProvider.RetrieveHistoryBeforeDateAsync(key, DateTime.Now);
            var entity = existingTableEntity.Result.FirstOrDefault();
            var existingEntity = JsonConvert.DeserializeObject<TModel>(entity?.Json);
            return existingEntity;
        }

        private PatchDocument CalculateJsonDiff(string existingObject, string newObject)
        {
            var existing = JToken.Parse(existingObject);
            var newOne = JToken.Parse(newObject);
            return new JsonDiffer().Diff(existing, newOne, false);
        }

        private async Task EventSourcingCreateRecord(TModel newModel)
        {
            var patchDocument = CalculateJsonDiff("".ToJson(), newModel.ToJson());
            await PersistEventSourcingRecord(newModel.Key, patchDocument, newModel);
        }

        private async Task EventSourcingUpdateRecord(TModel existingModel, TModel newModel)
        {
            PatchDocument patchDocument = CalculateJsonDiff(existingModel.ToJson(), newModel.ToJson());
            await PersistEventSourcingRecord(newModel.Key, patchDocument, newModel);
        }

        private async Task EventSourcingDeleteRecord(TModel existingModel, bool isSoftDelete)
        {
            PatchDocument patchDocument;

            if (isSoftDelete)
            {
                existingModel.EntityStatus = EntityStatus.Deleted;
                patchDocument = CalculateJsonDiff(existingModel.ToJson(), existingModel.ToJson());
            }
            else
            {
                existingModel.EntityStatus = EntityStatus.Deleted;
                patchDocument = CalculateJsonDiff(existingModel.ToJson(), "".ToJson());
            }

            await PersistEventSourcingRecord(existingModel.Key, patchDocument, existingModel);
        }

        private async Task PersistEventSourcingRecord(string partitionKey, PatchDocument patchDocument, TModel model)
        {
            EventSourcingTableStorageEntity eventSourcingEntity = new EventSourcingTableStorageEntity();
            eventSourcingEntity.Operations = patchDocument.Operations.ToJson(); //TODO: operations komen nog niet goed door.
            eventSourcingEntity.PartitionKey = partitionKey.Replace("/", "");
            eventSourcingEntity.Json = model.ToJson();
            eventSourcingEntity.ETag = model.ETag;

            await _historyPersistenceProvider.PersistAsync(eventSourcingEntity);
        }

        private void SetETag(TModel modelToUpdate)
        {
            //Create the hash with an empty ETag so it doesn't alter with the result.
            modelToUpdate.ETag = "";
            modelToUpdate.ETag = CreateMD5(modelToUpdate.ToJson());
        }

        private static string CreateMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();

                foreach (var hashByte in hashBytes)
                {
                    sb.Append(hashByte.ToString("X2"));
                }

                return sb.ToString();
            }
        }
    }
}
