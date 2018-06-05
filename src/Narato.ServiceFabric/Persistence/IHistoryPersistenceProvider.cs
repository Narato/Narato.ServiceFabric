using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Narato.ServiceFabric.Persistence
{
    public interface IHistoryPersistenceProvider<T> : IHistoryProvider<T>
        where T : TableEntity, new()
    {
        Task PersistAsync(TableEntity tableEntity);
    }
}
