using System;
using System.Diagnostics;
using System.Fabric;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.Client;

namespace Narato.ServiceFabric.Services.Wrappers
{
    public class ServiceRemotingClientWrapper : IServiceRemotingClient
    {
        private readonly IServiceRemotingClient _inner;
        public ResolvedServicePartition ResolvedServicePartition { get; set; }
        public string ListenerName { get; set; }
        public ResolvedServiceEndpoint Endpoint { get; set; }

        public ServiceRemotingClientWrapper(IServiceRemotingClient inner)
        {
            _inner = inner;
            ResolvedServicePartition = inner.ResolvedServicePartition;
            ListenerName = inner.ListenerName;
            Endpoint = inner.Endpoint;
        }

        public async Task<IServiceRemotingResponseMessage> RequestResponseAsync(IServiceRemotingRequestMessage requestMessage)
        {
            SetCorrelationId(requestMessage);

            return await _inner.RequestResponseAsync(requestMessage);
        }

        public void SendOneWay(IServiceRemotingRequestMessage requestMessage)
        {
            SetCorrelationId(requestMessage);
            _inner.SendOneWay(requestMessage);
        }

        private static string SetCorrelationId(IServiceRemotingRequestMessage requestMessage)
        {
            string correlationId;

            if (Activity.Current != null)
                correlationId = Activity.Current.RootId;
            else
                correlationId = (string)CallContext.GetData(Constants.CorrelationId) ?? Guid.NewGuid().ToString();

            requestMessage.GetHeader().AddHeader(Constants.CorrelationId, Encoding.ASCII.GetBytes(correlationId));

            return correlationId;
        }
    }
}
