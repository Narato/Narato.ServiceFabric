using System.Runtime.Serialization;
using Narato.ServiceFabric.Models;
using Newtonsoft.Json;

namespace Narato.ServiceFabric.Contracts.Models
{
    public class Ship : ModelBase
    {
        [DataMember]
        [JsonProperty("name")]
        public string Name { get; set; }
        [DataMember]
        [JsonProperty("location")]
        public string Location { get; set; }
        [DataMember]
        [JsonProperty("cargoweight")]
        public int CargoWeight { get; set; }

        protected override string GetKey()
        {
            return base.Id;
        }
    }
}
