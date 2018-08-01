using Narato.ServiceFabric.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Narato.ServiceFabric.Persistence
{
    public interface IEventSourcedPersistenceProvider<T>
        where T : ModelBase, new()
    {
        Task<EventSourcingTableStorageEntity> PersistAndReturnEntityAsync(T model);
    }
}
