using Narato.ResponseMiddleware.Models.Exceptions;
using Narato.ServiceFabric.Models;
using Narato.ServiceFabric.Persistence;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Narato.ResponseMiddleware.Models.Models;

namespace Narato.ServiceFabric.Services
{
    public class StatelessCrudServiceBase<TModel> : StatelessServiceBase
        where TModel : ModelBase, new()
    {
        protected readonly IPersistenceProvider<TModel> _provider;
        private readonly bool _softDeleteEnabled;

        protected StatelessCrudServiceBase(StatelessServiceContext serviceContext, IPersistenceProvider<TModel> provider, bool softDeleteEnabled) : base(serviceContext)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _softDeleteEnabled = softDeleteEnabled;
        }

        public virtual async Task<TModel> CreateAsync(TModel modelToCreate)
        {
            modelToCreate.Key = modelToCreate.GetKey();
            TModel entity = null;
            
            entity = await _provider.RetrieveAsync(modelToCreate.Key);

            if (entity != null)
                throw new ExceptionWithFeedback("EAE", $"The entity of type '{typeof(TModel).Name}' with key '{modelToCreate.Key}' already exists."); //todo update to EntityAlreadyExists exception

            await _provider.PersistAsync(modelToCreate);

            return await GetAsync(modelToCreate.Key);
        }

        public virtual async Task<TModel> UpdateAsync(TModel modelToUpdate)
        {
            var entity = await _provider.RetrieveAsync(modelToUpdate.Key);

            if (entity == null)
                throw new EntityNotFoundException("ENF", $"Entity with key '{modelToUpdate.Key}' was not found.");

            if (entity.Key != modelToUpdate.GetKey())
            {
                var modelDictionary = new ModelValidationDictionary<string>() { };
                modelDictionary.Add("key", "Key is immutable.");
                throw new ValidationException<string>(modelDictionary);
            }

            modelToUpdate.Id = entity.Id;

            await _provider.PersistAsync(modelToUpdate);

            return await GetAsync(modelToUpdate.Key);
        }

        public virtual async Task DeleteAsync(string key)
        {
            if (_softDeleteEnabled)
            {
                var entity = await GetAsync(key);
                entity.EntityStatus = EntityStatus.Deleted;
                entity.StatusChangedAt = DateTime.UtcNow;

                await _provider.PersistAsync(entity);
            }
            else
            {
                await _provider.DeleteAsync(key);
            }
        }

        public virtual async Task<TModel> GetAsync(string key)
        {
            var entity = await _provider.RetrieveAsync(key);

            if (entity == null)
                throw new EntityNotFoundException("ENF", $"Entity with key '{key}' was not found.");

            return entity;
        }

        public virtual async Task<List<TModel>> GetAllAsync()
        {
            if (_softDeleteEnabled)
            {
                var entities = (await _provider.RetrieveAllAsync()).Where(e => e.EntityStatus == EntityStatus.Active);

                return entities.ToList();
            }
            else
            {
                var entities = await _provider.RetrieveAllAsync();
                //TODO: check for entity == null        
                return entities.ToList();
            }
        }
        
        public virtual async Task<List<TModel>> GetHistoryAsync(string key)
        {
            if (_provider is IHistoryProvider)
            {
                var results = await ((IHistoryProvider) _provider).RetrieveHistoryAsync(key);
                var castResults = new List<TModel>();
                
                foreach (var result in results)
                {
                    castResults.Add(JsonConvert.DeserializeObject<TModel>(result.Json));
                }

                return castResults;
            }

            throw new NotImplementedException("The provider is not of type IHistoryProvider, method GetHistoryAsync is not available");
        }
        
        public virtual async Task<TModel> GetHistoryBeforeOrOnDateAsync(string key, DateTime date)
        {
            if (_provider is IHistoryProvider)
            {
                var result = await ((IHistoryProvider) _provider).RetrieveHistoryBeforeDateAsync(key, date);
                var entity = result.FirstOrDefault();
            
                if (entity == null)
                    throw new EntityNotFoundException("ENF", $"Entity with key '{key}' was not found with a timestamp on or before the given datetime '{date}'.");
            
                var existingEntity = JsonConvert.DeserializeObject<TModel>(entity.Json);
                return existingEntity;

            }

            throw new NotImplementedException("The provider is not of type IHistoryProvider, method GetHistoryBeforeOrOnDateAsync is not available");
        }
    }
}
