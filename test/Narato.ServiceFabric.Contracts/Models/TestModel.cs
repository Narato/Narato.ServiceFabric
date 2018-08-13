using System.Runtime.Serialization;
using Narato.ServiceFabric.Models;
using Newtonsoft.Json;

namespace Narato.ServiceFabric.Contracts.Models
{
    public class TestModel : ModelBase
    {
        [DataMember]
        [JsonProperty("testScenario")]
        public string TestScenario { get; set; }
        [DataMember]
        [JsonProperty("dummyProp")]
        public string DummyProp { get; set; }

        protected override string CreateKey()
        {
            return Id;
        }
    }
}
