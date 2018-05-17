using System.Fabric;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.Runtime;

namespace Narato.ServiceFabric.Services
{
    public class CustomServiceRemotingDispatcher : ServiceRemotingMessageDispatcher
    {
        public CustomServiceRemotingDispatcher(ServiceContext serviceContext, IService service) : base(serviceContext, service)
        {
        }

        public override Task<IServiceRemotingResponseMessage> HandleRequestResponseAsync(IServiceRemotingRequestContext requestContext, IServiceRemotingRequestMessage requestMessage)
        {
            requestMessage.GetHeader().TryGetHeaderValue(Constants.CorrelationId, out var correlationId);

            if (correlationId != null)
                CallContext.SetData(Constants.CorrelationId, Encoding.Default.GetString(correlationId));

            return base.HandleRequestResponseAsync(requestContext, requestMessage);
        }

    }
}
