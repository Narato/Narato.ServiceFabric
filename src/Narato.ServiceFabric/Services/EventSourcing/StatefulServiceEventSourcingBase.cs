using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Narato.ServiceFabric.ApplicationInsights;

namespace Narato.ServiceFabric.Services.EventSourcing
{
    //TODO: Is deze klasse nodig in het event sourcing verhaal??

    public class StatefulServiceEventSourcingBase : StatefulService, IService
    {
        public StatefulServiceEventSourcingBase(StatefulServiceContext serviceContext) : base(serviceContext)
        {
        }

        public StatefulServiceEventSourcingBase(StatefulServiceContext serviceContext, IReliableStateManagerReplica2 reliableStateManagerReplica) : base(serviceContext, reliableStateManagerReplica)
        {
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
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

        protected virtual async Task PersistData<T>(string dictionaryKey, string elementKey, T state)
            where T : class
        {
            var dictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, T>>(dictionaryKey);

            using (var tx = this.StateManager.CreateTransaction())
            {
                await dictionary.AddOrUpdateAsync(tx, elementKey, state, (key, value) => state);

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }
        }

        protected virtual async Task<T> RetrieveData<T>(string dictionaryKey, string elementKey)
            where T : class
        {
            var dictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, T>>(dictionaryKey);

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await dictionary.TryGetValueAsync(tx, elementKey);
                T state = null;

                if (result.HasValue)
                {
                    state = result.Value;
                }

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();

                return state;

            }
        }

    }
}
