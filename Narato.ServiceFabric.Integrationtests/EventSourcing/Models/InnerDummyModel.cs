using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Narato.ServiceFabric.Integrationtests.EventSourcing.Models
{
    public class InnerDummyModel
    {
        [DataMember]
        [JsonProperty("innerName")]
        public string InnerName { get; set; }
    }
}