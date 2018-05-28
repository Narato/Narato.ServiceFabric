using System;
using System.Runtime.Serialization;
using Microsoft.WindowsAzure.Storage.Table;
using Narato.ServiceFabric.Models.Interfaces;
using Newtonsoft.Json;

namespace Narato.ServiceFabric.Models
{
    public class TableStorageModelBase : TableEntity, IModelBase
    {
        public TableStorageModelBase()
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
        public DateTime ETag { get; set; }

        [DataMember]
        private string _key;

        protected virtual string GetKey()
        {
            return this._key;
        }

        public string Key
        {
            get { return GetKey(); }
            set { _key = value; }
        }
    }
}
