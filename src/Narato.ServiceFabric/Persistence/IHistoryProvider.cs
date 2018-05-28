using System.Threading.Tasks;
using Narato.ServiceFabric.Models.Interfaces;

namespace Narato.ServiceFabric.Persistence
{
    public interface IHistoryProvider<T>
        where T : IModelBase, new()
    {
        Task RetrieveHistoryAsync(string key);
    }
}
