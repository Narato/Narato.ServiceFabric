using System;
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
        //private TransactionScope scope;

        //[SetUp]
        //public void SetUp()
        //{
        //    scope = new TransactionScope();

        //}

        //[TearDown]
        //public void TearDown()
        //{
        //    scope.Dispose();
        //}

        [Fact]
        public async void InsertModel()
        {
            var service = GetService();

            DummyModel dummy = new DummyModel();
            dummy.Key = "Nieuw model";
            dummy.ETag = new DateTime(2018, 6, 4, 16, 59, 6).ToString(); //DateTime.Now;
            dummy.EntityStatus = EntityStatus.Active;
            dummy.Name = "New";
            dummy.Inner.InnerName = "newInner";

            var createdDummy = await service.Create(dummy);

            Assert.NotEqual(null, createdDummy);
        }

        [Fact]
        public async void UpdateModel()
        {
            var service = GetService();
            var dummy = await service.Get("Nieuw model");
            dummy.Name = "updated";
            dummy.Inner.InnerName = "UpdatedInner";

            var updatedDummy = await service.Update(dummy);


            Assert.NotEqual(null, updatedDummy);
        }

        private static ServiceWrapper GetService()
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
                    false);
            return service;
        }
    }

    

    
}
