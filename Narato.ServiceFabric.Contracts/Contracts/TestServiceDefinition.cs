using System;
using System.Collections.Generic;
using System.Text;
using Narato.ServiceFabric.Services;

namespace Narato.ServiceFabric.Contracts.Contracts
{
    public class TestServiceDefinition : ServiceDefinition
    {
        public override string ServiceTypeName => "Narato.ServiceFabric.TestService";
    }
}
