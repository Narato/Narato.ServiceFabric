using Microsoft.Azure.ServiceBus;
using Narato.ResponseMiddleware.Models.Exceptions;
using Narato.ServiceFabric.Models;
using System;
using System.Threading.Tasks;

namespace Narato.ServiceFabric.ServiceBus
{
    public class TopicSender : ITopicSender
    {
        private readonly TopicClient _topicClient;

        public TopicSender(string connectionString, string topicName)
        {
            _topicClient = new TopicClient(connectionString, topicName);
        }

        public async Task SendMessageAsync(TopicMessage message)
        {
            try
            {
                await _topicClient.SendAsync(new Message(ObjectSerializer.Serialize(message)));
            }
            catch (Exception e)
            {
                throw new ExceptionWithFeedback("EWF", "Something went wrong while posting to the servicebus: " + e.Message);
            }
        }

    }
}
