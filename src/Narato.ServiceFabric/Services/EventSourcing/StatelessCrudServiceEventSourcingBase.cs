using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading.Tasks;
using JsonDiffPatch;
using Narato.ResponseMiddleware.Models.Exceptions;
using Narato.ServiceFabric.Models;
using Narato.ServiceFabric.Persistence;
using Narato.StringExtensions;
using Newtonsoft.Json.Linq;

namespace Narato.ServiceFabric.Services.EventSourcing
{
    public abstract class StatelessCrudServiceEventSourcingBase<TModel> : StatelessCrudServiceBase<TModel>
        where TModel : ModelBase, new()
    {
        private readonly IHistoryPersistenceProvider<TModel> _provider;
        private readonly IPersistenceProvider<PatchDocument> _eventSourcingProvider;
        private readonly bool _softDeleteEnabled;

        protected StatelessCrudServiceEventSourcingBase(StatelessServiceContext serviceContext, IPersistenceProvider<TModel> provider, IPersistenceProvider<TModel> eventSourcingProvider, bool softDeleteEnabled) 
            : base(serviceContext, provider, softDeleteEnabled)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _eventSourcingProvider = eventSourcingProvider;
            _softDeleteEnabled = softDeleteEnabled;
        }

        public new virtual async Task<TModel> Create(TModel modelToCreate)
        {
            var newModel = await base.Create(modelToCreate);

            if (newModel != null)
            {
                var patchDocument = CalculateJsonDiff("".ToJson(), newModel.ToJson());
                await _eventSourcingProvider.PersistAsync(patchDocument);
            }

            return newModel;
        }

        public new virtual async Task<TModel> Update(TModel modelToUpdate)
        {
            var updatedModel = await base.Update(modelToUpdate);

            //TODO: update event table. Buth when the etag is different from the current e-tag, we throw an exception. This is not allowed
            //TODO: When doing a patch, the last one wins since we are only updating a part of the object and probably do not have access to the etag
            //TODO: to detect a patch, check the e-tag for a null value

            return updatedModel;
        }

        public new virtual async Task Delete(string key)
        {
            await base.Delete(key);

            //TODO: update event table
            //TODO: SoftDelete = update property
            //TODO: Hard delete = set object rawvalue to null
        }

        public new virtual async Task<TModel> Get(string key)
        {
            return await base.Get(key);
        }

        public new virtual async Task<List<TModel>> GetAll()
        {
            return await base.GetAll();
        }

        //TODO: Narato.JsonDiffPatch gebruiken zodat we oude en nieuwe waarde hebben?
        private PatchDocument CalculateJsonDiff(string existingObject, string newObject)
        {
            var existing = JToken.Parse(existingObject);
            var newOne = JToken.Parse(newObject);
            return new JsonDiffer().Diff(existing, newOne, false);
        }
    }
}
