using System;

namespace Narato.ServiceFabric.Services
{
    public abstract class ServiceDefinition
    {
        public abstract string ServiceTypeName { get; }

        public string InstanceKey { get; protected set; }

        private string _application = Environment.GetEnvironmentVariable("Fabric_ApplicationName");
        public string ApplicationName { protected get => _application; set => _application = "fabric:/" + value; }

        public virtual Uri ApplicationUri => new Uri(ApplicationName ?? "");
        public virtual Uri ServiceUri => string.IsNullOrEmpty(InstanceKey) ?
            new Uri(ApplicationUri.AbsoluteUri + $"/{ServiceTypeName}") :
            new Uri(ApplicationUri.AbsoluteUri + $"/{ServiceTypeName}/{InstanceKey.ToLower()}");
    }
}