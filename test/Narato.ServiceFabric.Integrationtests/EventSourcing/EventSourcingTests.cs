using System;
using System.Linq;
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
            DummyModel dummy = new DummyModel();
            dummy.EntityStatus = EntityStatus.Active;
            dummy.Name = "New";
            dummy.Inner.InnerName = "newInner";

            var createdDummy = await _service.CreateAsync(dummy);
            var history = await _service.GetHistoryAsync(dummy.Key);
            
            Assert.Equal(1, history.Count());
            Assert.NotEqual(null, createdDummy);
        }

        [Fact]
        public async void UpdateModel()
        {
            var dummy = await _service.GetAsync("nieuw model");
            dummy.Name = "updated";
            dummy.Inner.InnerName = "UpdatedInner";

            var updatedDummy = await _service.UpdateAsync(dummy);
            var history = await _service.GetHistoryAsync(dummy.Key);
            
            Assert.Equal(2, history.Count());
            Assert.NotEqual(null, updatedDummy);
        }
        
        [Fact]
        public async void GetInitialModel()
        {      
            var historyModel = await _service.GetHistoryBeforeOrOnDateAsync("Nieuw model", new DateTime(2018, 6, 13, 11, 48, 45));

            Assert.NotEqual(null, historyModel);
        }
        
        [Fact]
        public async void SoftDeleteModel()
        {
            await _service.DeleteAsync("nieuw model");

            var model = await _service.GetAsync("nieuw model");
            var history = await _service.GetHistoryAsync("nieuw model");
            
            Assert.True(history.Count() > 1);
            Assert.Equal(EntityStatus.Deleted, model.EntityStatus);
        }
        
        [Fact]
        public async void HardDeleteModel()
        {
            _service = GetService(false);
            await _service.DeleteAsync("nieuw model");
            var history = await _service.GetHistoryAsync("nieuw model");
            
            Assert.True(history.Count() > 1);
            await Assert.ThrowsAsync<EntityNotFoundException>(() =>  _service.GetAsync("Nieuw model"));
        }

        private static ServiceWrapper GetService(bool softdeleteEnabled = true)
        {
            ServiceWrapper service =
                new ServiceWrapper(
                    MockStatelessServiceContextFactory.Default,
                    new EventSourcedPersistenceProvider<DummyModel>("https://db-tt-nexus-dev.documents.azure.com:443/",
                        "TiJJMqAdUQXh1SWw2Zfo8wBW0hpfq4ljQjSuJJrbbetOnCFZ9UMJGVgqzOf67Op23l1ZlPPvhclLEEDJ0BP5hQ==",
                        "Models",
                        "ServiceFabricTest",
                        "DefaultEndpointsProtocol=https;AccountName=loadtestseventsourcing;AccountKey=2f/z5vxrHQM9DASJOfOtiFxngIEmbdJm/K+xAaI71nyysCSr9aMoDIVk/qV+SkEzKiuPf3UA7oXH6StYH0vjlw==;EndpointSuffix=core.windows.net",
                        "eventsourcing",
                        ""),
                    softdeleteEnabled);
            return service;
        }
    }
}