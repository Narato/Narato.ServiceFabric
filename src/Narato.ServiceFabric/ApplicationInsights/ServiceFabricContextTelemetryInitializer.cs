using System;
using System.Collections.Generic;
using System.Fabric;
using System.Text;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Narato.ServiceFabric.Services;

namespace Narato.ServiceFabric.ApplicationInsights
{
    public class ServiceFabricContextTelemetryInitializer : ITelemetryInitializer
    {
        private readonly ServiceContext _serviceContext;

        public ServiceFabricContextTelemetryInitializer(ServiceContext serviceContext)
        {
            this._serviceContext = serviceContext;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (!(telemetry is ISupportProperties enrichableTelemetry))
                return;

            telemetry.Context.Operation.Id = (string)CallContext.GetData(Constants.CorrelationId);
            telemetry.Context.Cloud.RoleName = _serviceContext.ServiceName.AbsolutePath;
            telemetry.Context.Cloud.RoleInstance = _serviceContext.NodeContext.NodeName;
            telemetry.Context.Component.Version = _serviceContext.CodePackageActivationContext.CodePackageVersion;

            if (!enrichableTelemetry.Properties.ContainsKey("SF_ServiceTypeName"))
                enrichableTelemetry.Properties.Add("SF_ServiceTypeName", _serviceContext.ServiceTypeName);
            if (!enrichableTelemetry.Properties.ContainsKey("SF_ReplicaInstanceId"))
                enrichableTelemetry.Properties.Add("SF_ReplicaInstanceId", _serviceContext.ReplicaOrInstanceId.ToString());
            if (!enrichableTelemetry.Properties.ContainsKey("SF_ApplicationTypeName"))
                enrichableTelemetry.Properties.Add("SF_ApplicationTypeName", _serviceContext.CodePackageActivationContext.ApplicationTypeName);
            if (!enrichableTelemetry.Properties.ContainsKey("SF_ApplicationName"))
                enrichableTelemetry.Properties.Add("SF_ApplicationName", _serviceContext.CodePackageActivationContext.ApplicationName);

            if (!enrichableTelemetry.Properties.ContainsKey("SF_PartitionId"))
            {
                if (_serviceContext is StatefulServiceContext statefulContext)
                    enrichableTelemetry.Properties.Add("SF_PartitionId", statefulContext.PartitionId.ToString());
                else
                    enrichableTelemetry.Properties.Add("SF_PartitionId", _serviceContext.PartitionId.ToString());
            }

        }
    }
}
