using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Narato.ServiceFabric.Models
{
    [DataContract]
    public abstract class ModelBase
    {
        public ModelBase()
        {
            EntityStatus = Models.EntityStatus.Active;
            StatusChangedAt = DateTime.UtcNow;
            CreatedAt = StatusChangedAt;
        }

        [DataMember]
        [JsonProperty("id")]
        public string Id { get; set; }

        [DataMember]
        [JsonProperty("entityStatus")]
        public string EntityStatus { get; set; }

        [DataMember]
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [DataMember]
        [JsonProperty("statusChangedAt")]
        public DateTime StatusChangedAt { get; set; }

        [DataMember]
        [JsonProperty("eTag")]
        public string ETag { get; set; }

        [DataMember]
        protected string _key; // protected so we can access it withing GetKey

        protected internal abstract string GetKey();

        // basically we want to guarantee that the key in the database is the same as the key produced by GetKey
        public string Key
        {
            get { return _key; }
            internal set { _key = value; } // internal so other assemblies can't call this directly
        }

    }
}
