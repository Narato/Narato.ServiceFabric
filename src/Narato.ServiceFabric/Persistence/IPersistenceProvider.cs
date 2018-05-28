using System.Collections.Generic;
using System.Threading.Tasks;
using Narato.ServiceFabric.Models.Interfaces;

namespace Narato.ServiceFabric.Persistence
{
    public interface IPersistenceProvider<T>
        where T : IModelBase, new()
    {
        Task PersistAsync(T model);
        Task<T> RetrieveAsync(string key);
        Task DeleteAsync(string key);
        Task DeleteAllAsync();

        Task<IEnumerable<T>> RetrieveAllAsync();
    }
}