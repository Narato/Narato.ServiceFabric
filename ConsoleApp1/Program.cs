using System;
using System.Threading.Tasks;
using Narato.ServiceFabric.Models;
using Narato.ServiceFabric.Persistence.DocumentDb;
using Narato.ServiceFabric.Persistence.TableStorage;
using Narato.ServiceFabric.Tests;
using Narato.ServiceFabric.Tests.EventSourcing.Models;
using Narato.ServiceFabric.Tests.EventSourcing.Wrappers;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //var task = Create();
            //task.Wait();

            var updateTask = Update();
            updateTask.Wait();

            Console.WriteLine("Finished");
            Console.ReadLine();
        }

        private static async Task Create()
        {
            try
            {
                var service = GetService();

                DummyModel dummy = new DummyModel();
                dummy.Key = "Nieuw model";
                dummy.ETag = new DateTime(2018, 6, 4, 16, 59, 6).ToString(); //DateTime.Now;
                dummy.EntityStatus = EntityStatus.Active;
                dummy.Name = "New";

                var createdDummy = await service.Create(dummy);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            } 
        }

        private static async Task Update()
        {
            try
            {
                var service = GetService();
                var dummy = await service.Get("Nieuw model");
                dummy.Name = "updated";

                var updatedDummy = await service.Update(dummy);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
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
