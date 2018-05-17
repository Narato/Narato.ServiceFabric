using Microsoft.ServiceFabric.Services.Remoting;
using Narato.ServiceFabric.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Narato.ServiceFabric.Services
{
    public interface ICrudService<TModel> : IService
        where TModel : ModelBase, new()
    {
        Task<TModel> Create(TModel modelToCreate);
        Task<TModel> Update(TModel modelToUpdate);
        Task Delete(string id);
        Task<TModel> Get(string id);
        Task<IEnumerable<TModel>> GetAll();
    }
}