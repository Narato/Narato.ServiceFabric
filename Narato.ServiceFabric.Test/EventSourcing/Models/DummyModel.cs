using System.Runtime.Serialization;
using Narato.ServiceFabric.Models;
using Newtonsoft.Json;

namespace Narato.ServiceFabric.Tests.EventSourcing.Models
{
    public class DummyModel : ModelBase
    {
        [DataMember]
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
