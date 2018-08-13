using System;
using System.Runtime.Serialization;
using Narato.ServiceFabric.Models;
using Newtonsoft.Json;

namespace Narato.ServiceFabric.Integrationtests.EventSourcing.Models
{
    public class DummyModel : ModelBase
    {
        [DataMember]
        [JsonProperty("name")]
        public string Name { get; set; }

        [DataMember]
        [JsonProperty("innerModel")]
        public InnerDummyModel Inner { get; set; }

        public DummyModel()
        {
            Inner = new InnerDummyModel();
        }

        protected override string GetKey()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
