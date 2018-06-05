using System.Fabric;
using Narato.ServiceFabric.Models;
using Narato.ServiceFabric.Persistence;
using Narato.ServiceFabric.Services.EventSourcing;
using Narato.ServiceFabric.Tests.EventSourcing.Models;

namespace Narato.ServiceFabric.Tests.EventSourcing.Wrappers
{
    public class ServiceWrapper : StatelessCrudServiceEventSourcingBase<DummyModel>
    {
        public ServiceWrapper(StatelessServiceContext serviceContext, IHistoryPersistenceProvider<EventSourcingTableStorageEntity> historyPersistenceProvider, IPersistenceProvider<DummyModel> dataPersistenceProvider, bool softDeleteEnabled) : base(serviceContext, historyPersistenceProvider, dataPersistenceProvider, softDeleteEnabled)
        {
        }
    }
}
