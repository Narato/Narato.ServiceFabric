using System.Fabric;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.ServiceFabric.Module;

namespace Narato.ServiceFabric.ApplicationInsights
{
    public class ServiceMonitor
    {
        public void Start(ServiceContext context)
        {
            //Will track each dependency call as a request. //Not needed for now
            //new ServiceRemotingRequestTrackingTelemetryModule().Initialize(configuration);
            new ServiceRemotingDependencyTrackingTelemetryModule().Initialize(TelemetryConfiguration.Active);

            //QuickPulseTelemtryProcessor is needed for Live metrics
            QuickPulseTelemetryProcessor processor = null;

            var telemetry = new ServiceFabricContextTelemetryInitializer(context);
            TelemetryConfiguration.Active.TelemetryInitializers.Add(telemetry);

            TelemetryConfiguration.Active.TelemetryProcessorChainBuilder
                .Use(next =>
                {
                    processor = new QuickPulseTelemetryProcessor(next);
                    return processor;
                })
                .Build();

            var quickPulse = new QuickPulseTelemetryModule();
            quickPulse.Initialize(TelemetryConfiguration.Active);
            quickPulse.RegisterTelemetryProcessor(processor);
        }
    }
}
