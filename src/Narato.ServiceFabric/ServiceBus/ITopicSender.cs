using System.Threading.Tasks;
using Narato.ServiceFabric.Models;

namespace Narato.ServiceFabric.ServiceBus
{
    public interface ITopicSender
    {
        Task SendMessageAsync(TopicMessage message);
    }
}
