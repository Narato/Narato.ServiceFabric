using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;
using Narato.ServiceFabric.Contracts.Models;

namespace Narato.ServiceFabric.Contracts.Contracts
{
    public interface ITestService : IService
    {
        Task<TestModel> CreateAsync(TestModel model);
        Task<TestModel> UpdateAsync(TestModel model);
        Task DeleteAsync(string key);
    }
}
