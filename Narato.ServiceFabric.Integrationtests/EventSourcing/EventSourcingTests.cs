using System;
using Narato.ResponseMiddleware.Models.Exceptions;
using Narato.ServiceFabric.Integrationtests.EventSourcing.Models;
using Narato.ServiceFabric.Integrationtests.EventSourcing.Wrappers;
using Narato.ServiceFabric.Integrationtests.MockHelpers;
using Narato.ServiceFabric.Models;
using Narato.ServiceFabric.Persistence.DocumentDb;
using Narato.ServiceFabric.Persistence.TableStorage;
using Xunit;

namespace Narato.ServiceFabric.Integrationtests.EventSourcing
{
    
    public class EventSourcingTests
    {
        
        /**************************************************************
         * WARNING! These are integration tests and will write data to the
         * databases configured in the GetService method. Be carefull not
         * to run these in a production environment!!
         **************************************************************/

        
        private ServiceWrapper _service;
        
        public EventSourcingTests()
        {
            _service = GetService();
        }
        
        [Fact]
        public async void InsertModel()
        {
            var service = GetService();

            DummyModel dummy = new DummyModel();
            dummy.Key = "Nieuw model";
            dummy.EntityStatus = EntityStatus.Active;
            dummy.Name = "New";
            dummy.Inner.InnerName = "newInner";

            var createdDummy = await service.CreateAsync(dummy);

            Assert.NotEqual(null, createdDummy);
        }

        [Fact]
        public async void UpdateModel()
        {
            var service = GetService();
            var dummy = await service.Get("Nieuw model");
            dummy.Name = "updated";
            dummy.Inner.InnerName = "UpdatedInner";

            var updatedDummy = await service.UpdateAsync(dummy);


            Assert.NotEqual(null, updatedDummy);
        }
        
        [Fact]
        public async void GetInitialModel()
        {      
            //TODO: only get when not deleted
            //var historyModel = _service.GetByDateAsync("Nieuw model", DateTime.Now.AddMinutes(-1));
            var historyModel = await _service.GetByDateAsync("Nieuw model", new DateTime(2018, 6, 7, 10, 48, 45));

            Assert.NotEqual(null, historyModel);
        }
        
        [Fact]
        public async void SoftDeleteModel()
        {
            await _service.DeleteAsync("Nieuw model");

            var model = await _service.Get("Nieuw model");
            Assert.Equal(EntityStatus.Deleted, model.EntityStatus);
        }
        
        [Fact]
        public async void HardDeleteModel()
        {
            _service = GetService(false);
            await _service.DeleteAsync("Nieuw model");

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>  _service.Get("Nieuw model"));
        }

        private static ServiceWrapper GetService(bool softdeleteEnabled = true)
        {
            ServiceWrapper service =
                new ServiceWrapper(
                    MockStatelessServiceContextFactory.Default,
                    new TableStoragePersistenceProvider<EventSourcingTableStorageEntity>(
                        "DefaultEndpointsProtocol=https;AccountName=nexuseventstorage;AccountKey=eiY0BC0pEQHOatyrO4vuw+//Ww/wLY8WiWr1bPvWxsCs+C8aSXwTfBttx7j9S/658MzDzWZY+qkMCb9giwFu2A==;EndpointSuffix=core.windows.net",
                        "NexusEvents"),
                    new DocumentDbPersistenceProvider<DummyModel>("https://db-tt-nexus-dev.documents.azure.com:443/",
                        "TiJJMqAdUQXh1SWw2Zfo8wBW0hpfq4ljQjSuJJrbbetOnCFZ9UMJGVgqzOf67Op23l1ZlPPvhclLEEDJ0BP5hQ==",
                        "NexusDB",
                        "NexusCore",
                        ""),
                    softdeleteEnabled);
            return service;
        }
    }
}
