using Narato.ServiceFabric.Models;
using System.Fabric.Description;
using System.Threading.Tasks;

namespace Narato.ServiceFabric.Services
{
    public interface IServiceManager
    {
        Task CreateStatefullServiceIfNotExists(ModelBase entity,
            string entityTypeRootPath,
            string serviceTypeName,
            int minReplicaSetSize = 3,
            int targetReplicaSetSize = 3,
            PartitionSchemeDescription partitionScheme = null
        );

        Task CreateStatelessServiceIfNotExists(ModelBase entity,
            string entityTypeRootPath,
            string serviceTypeName,
            PartitionSchemeDescription partitionScheme = null
        );

        Task<bool> ServiceExists(string serviceServiceUri);
    }
}