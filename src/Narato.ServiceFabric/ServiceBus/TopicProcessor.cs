using Microsoft.ApplicationInsights;
using Microsoft.Azure.ServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Narato.ServiceFabric.ServiceBus
{
    public class TopicProcessor
    {
        protected SubscriptionClient _subscriptionClient;
        protected TelemetryClient _telemetryClient;

        public TopicProcessor(string connectionString, string topicName, string subscriptionName, Func<Message, CancellationToken, Task> handler)
        {
            _subscriptionClient = new SubscriptionClient(connectionString, topicName, subscriptionName);
            _telemetryClient = new TelemetryClient();
            RegisterOnMessageHandlerAndReceiveMessages(handler);
        }

        private static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            return Task.CompletedTask;
        }

        protected void RegisterOnMessageHandlerAndReceiveMessages(Func<Message, CancellationToken, Task> handler)
        {
            // Configure the MessageHandler Options in terms of exception handling, number of concurrent messages to deliver etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`, set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                AutoComplete = false
            };

            // Register the function that will process messages
            _subscriptionClient.RegisterMessageHandler(handler, messageHandlerOptions);
        }
    }
}
