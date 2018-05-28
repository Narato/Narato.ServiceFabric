using System;

namespace Narato.ServiceFabric.Models.Interfaces
{
    public interface IModelBase
    {
        string Id { get; set; }
        string EntityStatus { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime StatusChangedAt { get; set; }
        DateTime ETag { get; set; }
        string Key { get; set; }
    }
}
