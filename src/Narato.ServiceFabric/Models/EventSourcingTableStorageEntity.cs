using System.Collections.Generic;
using JsonDiffPatch;

namespace Narato.ServiceFabric.Models
{
    public class EventSourcingTableStorageEntity : TableStorageModelBase
    {
        public string Operations { get; set; }
        public string Json { get; set; }
    }
}
