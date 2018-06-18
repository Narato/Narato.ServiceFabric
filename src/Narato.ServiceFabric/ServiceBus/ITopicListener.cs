using Microsoft.Azure.ServiceBus;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Narato.ServiceFabric.ServiceBus
{
    public interface ITopicListener : ICommunicationListener
    {
        void RegisterMessageHandler(Func<Message, CancellationToken, Task> value);
    }
}
