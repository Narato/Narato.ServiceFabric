using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Narato.ServiceFabric.Models;

namespace Narato.ServiceFabric.Persistence
{
    public interface IHistoryProvider
    {
        Task<IEnumerable<EventSourcingTableStorageEntity>> RetrieveHistoryAsync(string key);
        Task<IEnumerable<EventSourcingTableStorageEntity>> RetrieveHistoryBeforeDateAsync(string partitionKey, DateTime date);
    }
}