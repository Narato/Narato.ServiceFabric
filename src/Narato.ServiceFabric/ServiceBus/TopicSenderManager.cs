using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Narato.ServiceFabric.Models;
using System;
using System.Threading.Tasks;

namespace Narato.ServiceFabric.ServiceBus
{
    public class TopicSenderManager : ITopicSenderManager
    {
        private readonly string _queueName;
        readonly IReliableStateManager _stateManager;
        private readonly ITopicSender _topicSender;

        private bool _processing;

        public TopicSenderManager(ITopicSender topicSender, string queueName, IReliableStateManager stateManager)
        {
            _queueName = queueName;
            _stateManager = stateManager;
            _topicSender = topicSender;
        }

        public async Task EnqueueMessageAsync(TopicMessage topicMessage)
        {
            var reliableQueue = await _stateManager.GetOrAddAsync<IReliableConcurrentQueue<TopicMessage>>(_queueName);
            using (var tx = _stateManager.CreateTransaction())
            {
                await reliableQueue.EnqueueAsync(tx, topicMessage);
                await tx.CommitAsync();
            }
            StartProcessingQueue();
        }

        private void StartProcessingQueue()
        {
            if (!_processing)
            {
                _processing = true;
                Task.Run(async () => await ProcessQueueAsync());
            }
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                var reliableQueue = await _stateManager.GetOrAddAsync<IReliableConcurrentQueue<TopicMessage>>(_queueName);
                while (reliableQueue.Count > 0)
                {
                    using (var tx = _stateManager.CreateTransaction())
                    {
                        var result = await reliableQueue.TryDequeueAsync(tx);
                        if (result.HasValue)
                        {
                            await _topicSender.SendMessageAsync(result.Value);
                        }
                        await tx.CommitAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                //use logger
                Console.WriteLine(ex);
            }
            _processing = false;
        }

    }
}

