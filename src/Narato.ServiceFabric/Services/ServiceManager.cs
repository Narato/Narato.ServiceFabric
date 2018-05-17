using Narato.ServiceFabric.Models;
using System;
using System.Fabric;
using System.Fabric.Description;
using System.Threading.Tasks;

namespace Narato.ServiceFabric.Services
{
    public class ServiceManager : IServiceManager
    {
        public async Task CreateStatefullServiceIfNotExists(ModelBase entity,
            string entityTypeRootPath,
            string serviceTypeName,
            int minReplicaSetSize = 3,
            int targetReplicaSetSize = 3,
            PartitionSchemeDescription partitionScheme = null
        )
        {
            var client = new FabricClient();
            try
            {
                await client.ServiceManager.GetServiceDescriptionAsync(
                    new Uri($"fabric:/Dashboard/{entityTypeRootPath}/{entity.Key.ToLower()}"));
            }
            catch (FabricServiceNotFoundException)
            {
                await client.ServiceManager.CreateServiceAsync(new StatefulServiceDescription
                {
                    ApplicationName = new Uri("fabric:/Dashboard"),
                    ServiceName = new Uri($"fabric:/Dashboard/{entityTypeRootPath}/{entity.Key.ToLower()}"),
                    MinReplicaSetSize = minReplicaSetSize,
                    TargetReplicaSetSize = targetReplicaSetSize,
                    ServiceTypeName = serviceTypeName,
                    PartitionSchemeDescription = partitionScheme ?? new SingletonPartitionSchemeDescription(),
                    HasPersistedState = true
                });
            }
        }

        public async Task CreateStatelessServiceIfNotExists(ModelBase entity,
            string entityTypeRootPath,
            string serviceTypeName,
            PartitionSchemeDescription partitionScheme = null
        )
        {
            var client = new FabricClient();
            try
            {
                await client.ServiceManager.GetServiceDescriptionAsync(
                    new Uri($"fabric:/Dashboard/{entityTypeRootPath}/{entity.Key.ToLower()}"));
            }
            catch (FabricServiceNotFoundException)
            {
                await client.ServiceManager.CreateServiceAsync(new StatelessServiceDescription()
                {
                    ApplicationName = new Uri("fabric:/Dashboard"),
                    ServiceName = new Uri($"fabric:/Dashboard/{entityTypeRootPath}/{entity.Key.ToLower()}"),
                    ServiceTypeName = serviceTypeName,
                    PartitionSchemeDescription = partitionScheme ?? new SingletonPartitionSchemeDescription(),
                    InstanceCount = -1

                });
            }
        }

        public async Task<bool> ServiceExists(Uri serviceUri)
        {
            var client = new FabricClient();
            try
            {
                await client.ServiceManager.GetServiceDescriptionAsync(serviceUri);
                return true;
            }
            catch (FabricServiceNotFoundException)
            {
                return false;
            }
        }


        public async Task<bool> ServiceExists(string serviceUri)
        {
            var client = new FabricClient();
            try
            {
                await client.ServiceManager.GetServiceDescriptionAsync(
                    new Uri(serviceUri));
                return true;
            }
            catch (FabricServiceNotFoundException)
            {
                return false;
            }
        }
    }
}

