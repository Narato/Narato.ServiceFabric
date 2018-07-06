using System.Fabric;
using Narato.ServiceFabric.Contracts.Contracts;
using Narato.ServiceFabric.Contracts.Models;
using Narato.ServiceFabric.Services;
using Narato.ServiceFabric.TestService.Providers;

namespace Narato.ServiceFabric.TestService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class TestService : StatelessCrudServiceBase<TestModel>, ITestService
    {
        public TestService(StatelessServiceContext context, DocumentDbEventSourcingTestModelProvider provider, bool softDeleteEnabled)
            : base(context, provider, softDeleteEnabled)
        {
        }


    }
}
