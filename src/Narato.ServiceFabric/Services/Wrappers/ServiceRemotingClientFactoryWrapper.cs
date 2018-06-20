using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.Client;

namespace Narato.ServiceFabric.Services.Wrappers
{
    public class ServiceRemotingClientFactoryWrapper : IServiceRemotingClientFactory
    {
        private readonly IServiceRemotingClientFactory _inner;
        public event EventHandler<CommunicationClientEventArgs<IServiceRemotingClient>> ClientConnected;
        public event EventHandler<CommunicationClientEventArgs<IServiceRemotingClient>> ClientDisconnected;

        private readonly TelemetryClient _telemetryClient;

        public ServiceRemotingClientFactoryWrapper(IServiceRemotingClientFactory inner)
        {
            _inner = inner;
            _telemetryClient = new TelemetryClient(TelemetryConfiguration.Active);
        }

        public async Task<IServiceRemotingClient> GetClientAsync(Uri serviceUri, ServicePartitionKey partitionKey, TargetReplicaSelector targetReplicaSelector,
            string listenerName, OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            var client = await _inner.GetClientAsync(serviceUri, partitionKey, targetReplicaSelector, listenerName, retrySettings, cancellationToken).ConfigureAwait(false);
            return new ServiceRemotingClientWrapper(client);
        }

        public async Task<IServiceRemotingClient> GetClientAsync(ResolvedServicePartition previousRsp, TargetReplicaSelector targetReplicaSelector, string listenerName, OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            var client = await _inner.GetClientAsync(previousRsp, targetReplicaSelector, listenerName, retrySettings, cancellationToken).ConfigureAwait(false);
            return new ServiceRemotingClientWrapper(client);
        }

        public async Task<OperationRetryControl> ReportOperationExceptionAsync(IServiceRemotingClient client, ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            var exceptionTelemetry = new ExceptionTelemetry();
            exceptionTelemetry.Context.Operation.Id = CallContext.GetData(Constants.CorrelationId)?.ToString();
            exceptionTelemetry.Exception = exceptionInformation.Exception;
            exceptionTelemetry.Message = "An unhandled exception occured";

            _telemetryClient.TrackException(exceptionTelemetry);

            //Use the inner client since this is the one who does all the requests etc
            var wrapperClient = client as ServiceRemotingClientWrapper;
            var innerClient = await _inner.GetClientAsync(wrapperClient.ResolvedServicePartition, TargetReplicaSelector.Default, wrapperClient.ListenerName, retrySettings, cancellationToken);
            return await _inner.ReportOperationExceptionAsync(innerClient, exceptionInformation, retrySettings, cancellationToken);
        }

        public IServiceRemotingMessageBodyFactory GetRemotingMessageBodyFactory()
        {
            return this._inner.GetRemotingMessageBodyFactory();
        }
    }
}
