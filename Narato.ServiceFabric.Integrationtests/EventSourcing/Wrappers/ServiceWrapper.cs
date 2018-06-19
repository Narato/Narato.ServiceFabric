using System.Fabric;
using Narato.ServiceFabric.Integrationtests.EventSourcing.Models;
using Narato.ServiceFabric.Persistence;
using Narato.ServiceFabric.Services;

namespace Narato.ServiceFabric.Integrationtests.EventSourcing.Wrappers
{
    public class ServiceWrapper : StatelessCrudServiceBase<DummyModel>
    {
        public ServiceWrapper(StatelessServiceContext serviceContext, IPersistenceProvider<DummyModel> dataPersistenceProvider, bool softDeleteEnabled) : base(serviceContext, dataPersistenceProvider, softDeleteEnabled)
        {
        }
    }
}
