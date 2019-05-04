using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;
using Narato.ServiceFabric.Contracts.Models;

namespace Narato.ServiceFabric.Contracts.Contracts
{
    public interface IShipService : IService
    {
        Task<Ship> CreateAsync(Ship model);
        Task<Ship> UpdateAsync(Ship model);
        Task DeleteAsync(string key);
    }
}
