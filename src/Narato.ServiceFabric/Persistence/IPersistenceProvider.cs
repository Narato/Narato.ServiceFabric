using System.Collections.Generic;
using System.Threading.Tasks;
using Narato.ServiceFabric.Models;

namespace Narato.ServiceFabric.Persistence
{
    public interface IPersistenceProvider<T>
        where T : ModelBase, new()
    {
        Task PersistAsync(T model);
        Task<T> RetrieveAsync(string key);
        Task DeleteAsync(string key);

        Task<IEnumerable<T>> RetrieveAllAsync();
    }
}