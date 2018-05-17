using System.Threading.Tasks;
using Narato.ServiceFabric.Models;

namespace Narato.ServiceFabric.ServiceBus
{
    public interface ITopicSenderManager
    {
        Task EnqueueMessageAsync(TopicMessage serviceBusMessage);
    }
}
