using Microsoft.ServiceFabric.Services.Remoting;
using Narato.ServiceFabric.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Narato.ServiceFabric.Services
{
    public interface ICrudService<TModel> : IService
        where TModel : ModelBase, new()
    {
        Task<TModel> CreateAsync(TModel modelToCreate);
        Task<TModel> UpdateAsync(TModel modelToUpdate);
        Task DeleteAsync(string key);
        Task<TModel> GetAsync(string key);
        Task<List<TModel>> GetAllAsync(); // see https://github.com/Azure/service-fabric-issues/issues/735#issuecomment-384756849
        // TODO: "In 6.3 , we are adding support for interface types also."
    }
}