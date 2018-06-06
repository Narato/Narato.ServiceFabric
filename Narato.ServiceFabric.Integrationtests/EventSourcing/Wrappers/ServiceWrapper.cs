using System.Fabric;
using Narato.ServiceFabric.Integrationtests.EventSourcing.Models;
using Narato.ServiceFabric.Models;
using Narato.ServiceFabric.Persistence;
using Narato.ServiceFabric.Services.EventSourcing;

namespace Narato.ServiceFabric.Integrationtests.EventSourcing.Wrappers
{
    public class ServiceWrapper : StatelessCrudServiceEventSourcingBase<DummyModel>
    {
        public ServiceWrapper(StatelessServiceContext serviceContext, IHistoryPersistenceProvider<EventSourcingTableStorageEntity> historyPersistenceProvider, IPersistenceProvider<DummyModel> dataPersistenceProvider, bool softDeleteEnabled) : base(serviceContext, historyPersistenceProvider, dataPersistenceProvider, softDeleteEnabled)
        {
        }
    }
}
