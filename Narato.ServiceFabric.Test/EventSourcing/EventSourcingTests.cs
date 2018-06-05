using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Health;
using NUnit.Framework;
using System.Transactions;
using Narato.ServiceFabric.Models;
using Narato.ServiceFabric.Persistence.DocumentDb;
using Narato.ServiceFabric.Persistence.TableStorage;
using Narato.ServiceFabric.Services.EventSourcing;
using Narato.ServiceFabric.Tests.EventSourcing.Models;
using Narato.ServiceFabric.Tests.EventSourcing.Wrappers;

namespace Narato.ServiceFabric.Tests
{
    
    public class EventSourcingTests
    {
        //private TransactionScope scope;

        //[SetUp]
        //public void SetUp()
        //{
        //    scope = new TransactionScope();

        //}

        //[TearDown]
        //public void TearDown()
        //{
        //    scope.Dispose();
        //}

        [Test]
        public async void InsertModel()
        {
            var service = GetService();

            DummyModel dummy = new DummyModel();
            dummy.Key = "Nieuw model";
            dummy.ETag = new DateTime(2018, 6, 4, 16, 59, 6).ToString(); //DateTime.Now;
            dummy.EntityStatus = EntityStatus.Active;
            dummy.Name = "New";

            var createdDummy = await service.Create(dummy);


            Assert.AreNotEqual(createdDummy, null);
        }

        [Test]
        public async void UpdateModel()
        {


            var service = GetService();
            var dummy = await service.Get("Nieuw model");
            dummy.Name = "updated";

            var updatedDummy = await service.Update(dummy);


            Assert.AreNotEqual(updatedDummy, null);
        }

        private static ServiceWrapper GetService()
        {
            ServiceWrapper service =
                new ServiceWrapper(
                    MockStatelessServiceContextFactory.Default,
                    new TableStoragePersistenceProvider<EventSourcingTableStorageEntity>(
                        "DefaultEndpointsProtocol=https;AccountName=nexuseventstorage;AccountKey=eiY0BC0pEQHOatyrO4vuw+//Ww/wLY8WiWr1bPvWxsCs+C8aSXwTfBttx7j9S/658MzDzWZY+qkMCb9giwFu2A==;EndpointSuffix=core.windows.net",
                        "NexusEvents"),
                    new DocumentDbPersistenceProvider<DummyModel>("https://db-tt-nexus-dev.documents.azure.com:443/",
                        "TiJJMqAdUQXh1SWw2Zfo8wBW0hpfq4ljQjSuJJrbbetOnCFZ9UMJGVgqzOf67Op23l1ZlPPvhclLEEDJ0BP5hQ==",
                        "NexusDB",
                        "NexusCore",
                        ""),
                    false);
            return service;
        }
    }

    /// <summary>
    /// Factory that returns an instance of <see cref="StatelessServiceContext"/> using <see cref="MockCodePackageActivationContext.Default"/> or customized instance using <see cref="Create"/>.
    /// </summary>
    public class MockStatelessServiceContextFactory
    {
        public const string ServiceTypeName = "MockServiceType";
        public const string ServiceName = "fabric:/MockApp/MockStatefulService";

        /// <summary>
        /// Returns an instance of <see cref="StatelessServiceContext"/> using <see cref="MockCodePackageActivationContext.Default"/>, <see cref="ServiceTypeName"/>, <see cref="ServiceName"/> and random values for Partition and Instance id's.

        /// </summary>
        public static StatelessServiceContext Default { get; } = Create(
            MockCodePackageActivationContext.Default, ServiceTypeName,
            new Uri(ServiceName),
            Guid.NewGuid(),
            long.MaxValue
        );


        /// <summary>
        /// Returns an instance of <see cref="StatelessServiceContext"/> using using the specified arguments.
        /// <param name="codePackageActivationContext">Activation context</param>
        /// <param name="serviceTypeName">Name of the service type</param>
        /// <param name="serviceName">The URI that should be used by the ServiceContext</param>
        /// <param name="partitionId">PartitionId</param>
        /// <param name="instanceId">InstanceId</param>
        /// </summary>
        public static StatelessServiceContext Create(ICodePackageActivationContext codePackageActivationContext,
            string serviceTypeName,
            Uri serviceName,
            Guid partitionId,
            long instanceId)
        {
            return new StatelessServiceContext(
                new NodeContext("Node0", new NodeId(0, 1), 0, "NodeType1", "MOCK.MACHINE"),
                codePackageActivationContext,
                serviceTypeName,
                serviceName,
                null,
                partitionId,
                instanceId);
        }
    }

    /// <summary>
    /// Represents activation context for the Service Fabric activated service.
    /// </summary>
    /// <remarks>Includes information from the service manifest as well as information
    /// about the currently activated code package like work directory, context id etc.</remarks>
    public class MockCodePackageActivationContext : ICodePackageActivationContext
    {
        private bool _isDisposed;

        /// <summary>
        /// Returns a default instance, using mock values.
        /// </summary>
        public static ICodePackageActivationContext Default { get; } = new MockCodePackageActivationContext(
            "fabric:/MockApp",
            "MockAppType",
            "Code",
            "1.0.0.0",
            Guid.NewGuid().ToString(),
            @"C:\logDirectory",
            @"C:\tempDirectory",
            @"C:\workDirectory",
            "ServiceManifestName",
            "1.0.0.0"
        );



        public MockCodePackageActivationContext(
           string applicationName,
           string applicationTypeName,
           string codePackageName,
           string codePackageVersion,
           string context,
           string logDirectory,
           string tempDirectory,
           string workDirectory,
           string serviceManifestName,
           string serviceManifestVersion)
        {

            ApplicationName = applicationName;
            ApplicationTypeName = applicationTypeName;
            CodePackageName = codePackageName;
            CodePackageVersion = codePackageVersion;
            ContextId = context;
            LogDirectory = logDirectory;
            TempDirectory = tempDirectory;
            WorkDirectory = workDirectory;
            ServiceManifestName = serviceManifestName;
            ServiceManifestVersion = serviceManifestVersion;
        }

        public event EventHandler<PackageAddedEventArgs<CodePackage>> CodePackageAddedEvent;

        public event EventHandler<PackageModifiedEventArgs<CodePackage>> CodePackageModifiedEvent;

        public event EventHandler<PackageRemovedEventArgs<CodePackage>> CodePackageRemovedEvent;

        public event EventHandler<PackageAddedEventArgs<ConfigurationPackage>> ConfigurationPackageAddedEvent;

        public event EventHandler<PackageModifiedEventArgs<ConfigurationPackage>> ConfigurationPackageModifiedEvent;

        public event EventHandler<PackageRemovedEventArgs<ConfigurationPackage>> ConfigurationPackageRemovedEvent;

        public event EventHandler<PackageAddedEventArgs<DataPackage>> DataPackageAddedEvent;

        public event EventHandler<PackageModifiedEventArgs<DataPackage>> DataPackageModifiedEvent;

        public event EventHandler<PackageRemovedEventArgs<DataPackage>> DataPackageRemovedEvent;

        public string ApplicationName { get; set; }

        public ApplicationPrincipalsDescription ApplicationPrincipalsDescription { get; set; }
        public string ApplicationTypeName { get; set; }

        public CodePackage CodePackage { get; set; }
        public string CodePackageName { get; set; }

        public string CodePackageVersion { get; set; }

        public ConfigurationPackage ConfigurationPackage { get; set; }
        public List<string> ConfigurationPackageNames { get; set; }
        public string ContextId { get; set; }

        public DataPackage DataPackage { get; set; }
        public List<string> DataPackageNames { get; set; }
        public KeyedCollection<string, EndpointResourceDescription> EndpointResourceDescriptions { get; set; }
        public List<HealthInformation> HealthInformations { get; set; } = new List<HealthInformation>();
        public string LogDirectory { get; set; }

        public KeyedCollection<string, ServiceGroupTypeDescription> ServiceGroupTypes { get; set; }
        public string ServiceManifestName { get; set; }
        public string ServiceManifestVersion { get; set; }
        public KeyedCollection<string, ServiceTypeDescription> ServiceTypes { get; set; }
        public string TempDirectory { get; set; }

        public string WorkDirectory { get; set; }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }

        public ApplicationPrincipalsDescription GetApplicationPrincipals()
        {
            return ApplicationPrincipalsDescription;
        }

        public IList<string> GetCodePackageNames()
        {
            return new List<string>() { CodePackageName };
        }

        public CodePackage GetCodePackageObject(string packageName)
        {
            return CodePackage;
        }

        public IList<string> GetConfigurationPackageNames()
        {
            return ConfigurationPackageNames;
        }

        public ConfigurationPackage GetConfigurationPackageObject(string packageName)
        {
            return ConfigurationPackage;
        }

        public IList<string> GetDataPackageNames()
        {
            return DataPackageNames;
        }

        public DataPackage GetDataPackageObject(string packageName)
        {
            return DataPackage;
        }

        public EndpointResourceDescription GetEndpoint(string endpointName)
        {
            return EndpointResourceDescriptions[endpointName];
        }

        public KeyedCollection<string, EndpointResourceDescription> GetEndpoints()
        {
            return EndpointResourceDescriptions;
        }

        public KeyedCollection<string, ServiceGroupTypeDescription> GetServiceGroupTypes()
        {
            return ServiceGroupTypes;
        }

        public string GetServiceManifestName()
        {
            return ServiceManifestName;
        }

        public string GetServiceManifestVersion()
        {
            return ServiceManifestVersion;
        }

        public KeyedCollection<string, ServiceTypeDescription> GetServiceTypes()
        {
            return ServiceTypes;
        }

        public void OnCodePackageAddedEvent(PackageAddedEventArgs<CodePackage> e)
        {
            CodePackageAddedEvent?.Invoke(this, e);
        }

        public void OnCodePackageModifiedEvent(PackageModifiedEventArgs<CodePackage> e)
        {
            CodePackageModifiedEvent?.Invoke(this, e);
        }

        public void OnCodePackageRemovedEvent(PackageRemovedEventArgs<CodePackage> e)
        {
            CodePackageRemovedEvent?.Invoke(this, e);
        }

        public void OnConfigurationPackageAddedEvent(PackageAddedEventArgs<ConfigurationPackage> e)
        {
            ConfigurationPackageAddedEvent?.Invoke(this, e);
        }

        public void OnConfigurationPackageModifiedEvent(PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            ConfigurationPackageModifiedEvent?.Invoke(this, e);
        }

        public void OnConfigurationPackageRemovedEvent(PackageRemovedEventArgs<ConfigurationPackage> e)
        {
            ConfigurationPackageRemovedEvent?.Invoke(this, e);
        }

        public void OnDataPackageAddedEvent(PackageAddedEventArgs<DataPackage> e)
        {
            DataPackageAddedEvent?.Invoke(this, e);
        }

        public void OnDataPackageModifiedEvent(PackageModifiedEventArgs<DataPackage> e)
        {
            DataPackageModifiedEvent?.Invoke(this, e);
        }

        public void OnDataPackageRemovedEvent(PackageRemovedEventArgs<DataPackage> e)
        {
            DataPackageRemovedEvent?.Invoke(this, e);
        }

        public void ReportApplicationHealth(HealthInformation healthInformation)
        {
            HealthInformations?.Add(healthInformation);
        }

        public void ReportDeployedApplicationHealth(HealthInformation healthInformation)
        {
            HealthInformations?.Add(healthInformation);
        }

        public void ReportDeployedServicePackageHealth(HealthInformation healthInformation)
        {
            HealthInformations?.Add(healthInformation);
        }

        public void ReportApplicationHealth(HealthInformation healthInformation, HealthReportSendOptions sendOptions)
        {
            HealthInformations?.Add(healthInformation);
        }

        public void ReportDeployedApplicationHealth(HealthInformation healthInformation, HealthReportSendOptions sendOptions)
        {
            HealthInformations?.Add(healthInformation);
        }

        public void ReportDeployedServicePackageHealth(HealthInformation healthInformation, HealthReportSendOptions sendOptions)
        {
            HealthInformations?.Add(healthInformation);
        }
    }
}
