using System;
using System.Collections.Generic;
using System.Text;
using Narato.ServiceFabric.Services;

namespace Narato.ServiceFabric.Contracts.Contracts
{
    public class ShipServiceDefinition : ServiceDefinition
    {
        public override string ServiceTypeName => "Narato.ServiceFabric.TestService";
    }
}
