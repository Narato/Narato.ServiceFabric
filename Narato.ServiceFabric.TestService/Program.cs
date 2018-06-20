using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.ServiceFabric.Services.Runtime;
using Narato.ServiceFabric.TestService.Providers;

namespace Narato.ServiceFabric.TestService
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // The ServiceManifest.XML file defines one or more service type names.
                // Registering a service maps a service type name to a .NET type.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.

                ServiceRuntime.RegisterServiceAsync("Narato.ServiceFabric.TestServiceType",
                    context =>
                    {
                        var configurationPackage = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
                        var docDbEndpoint = configurationPackage.Settings.Sections["MyConfigSection"].Parameters["docdbEndPoint"].Value;
                        var docdbAuthKey = configurationPackage.Settings.Sections["MyConfigSection"].Parameters["docdbAuthKey"].Value;
                        var docdbDatabase = configurationPackage.Settings.Sections["MyConfigSection"].Parameters["docdbDatabase"].Value;
                        var docdbCollection = configurationPackage.Settings.Sections["MyConfigSection"].Parameters["docdbCollection"].Value;
                        var tableStorageConnectionString = configurationPackage.Settings.Sections["MyConfigSection"].Parameters["tableStorageConnectionString"].Value;
                        var tableStorageTableName = configurationPackage.Settings.Sections["MyConfigSection"].Parameters["tableStorageTableName"].Value;
                        var tableStorageAuthenticationName = configurationPackage.Settings.Sections["MyConfigSection"].Parameters["tableStorageAuthenticationName"].Value;

                        var provider = new DocumentDbEventSourcingTestModelProvider(docDbEndpoint, docdbAuthKey, docdbDatabase, docdbCollection, tableStorageConnectionString, tableStorageTableName, tableStorageAuthenticationName);

                        return new TestService(context, provider, true);
                    }).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(TestService).Name);

                // Prevents this host process from terminating so services keep running.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
