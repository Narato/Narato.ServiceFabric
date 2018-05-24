using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using System;
using Narato.ServiceFabric.Services.Wrappers;

namespace Narato.ServiceFabric.Services
{
    public class ServiceResolver
    {
        private readonly ServiceProxyFactory _proxyFactory;

        public ServiceResolver()
        {
            _proxyFactory = new ServiceProxyFactory((c) => new ServiceRemotingClientFactoryWrapper(new FabricTransportServiceRemotingClientFactory()));
        }

        public virtual TServiceType Resolve<TServiceType>(ServiceDefinition serviceDefinition) where TServiceType : IService
        {
            return _proxyFactory.CreateServiceProxy<TServiceType>(serviceDefinition.ServiceUri, ServicePartitionKey.Singleton);
        }

        public virtual TServiceType Resolve<TServiceType>(Uri serviceUri) where TServiceType : IService
        {
            return _proxyFactory.CreateServiceProxy<TServiceType>(serviceUri, ServicePartitionKey.Singleton);
        }
    }
}