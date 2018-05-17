using Narato.ServiceFabric.Models;
using Newtonsoft.Json;
using System;

namespace Narato.ServiceFabric.Persistence
{
    public class PersistedModel<T> where T : ModelBase, new()
    {
        [JsonProperty("id")]
        public string Id
        {
            get { return Current?.Id; }
            set { if (Current != null) { Current.Id = value; } }
        }

        [JsonProperty("key")]
        public string Key
        {
            get { return Current?.Key; }
        }

        [JsonProperty("type")]
        public string Type => typeof(T).Name;

        [JsonProperty("_self")]
        public string Self { get; set; }

        [JsonProperty("current")]
        public T Current { get; set; }

        public PersistedModel()
        {
            Current = new T();
        }

        public PersistedModel(T model)
        {
            Current = model;
            if (Current.Id == null)
            {
                var guid = Guid.NewGuid().ToString("D");
                Current.Id = guid;
                //this.Id = guid;
            }
        }

    }
}
