using Narato.ServiceFabric.Models;

namespace Narato.ServiceFabric.Persistence
{
    public interface IHistoryPersistenceProvider<T> : IHistoryProvider<T>, IPersistenceProvider<T>
        where T : ModelBase, new()
    {
    }
}
