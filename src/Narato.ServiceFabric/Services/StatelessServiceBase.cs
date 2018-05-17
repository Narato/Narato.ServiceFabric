using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Narato.ServiceFabric.ApplicationInsights;

namespace Narato.ServiceFabric.Services
{
    public class StatelessServiceBase : StatelessService, IService
    {
        public StatelessServiceBase(StatelessServiceContext serviceContext) : base(serviceContext)
        {
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                new ServiceInstanceListener((c) => new FabricTransportServiceRemotingListener(c, this))
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await InitializeService();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }


        protected virtual async Task InitializeService()
        {
            new ServiceMonitor().Start(Context);
        }

        protected virtual string GetConfigItem(string sectionName, string propertyName)
        {
            var configurationPackage = Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            return configurationPackage.Settings.Sections[sectionName].Parameters[propertyName].Value;
        }
    }
}

