using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Narato.ServiceFabric.Persistence
{
    public interface IHistoryProvider<T>
        where T : TableEntity, new()
    {
        Task<IEnumerable<T>> RetrieveHistoryAsync(string key);
        Task<IEnumerable<T>> RetrieveHistoryBeforeDateAsync(string partitionKey, DateTime date);
    }
}