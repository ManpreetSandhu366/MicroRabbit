using MediatR;
using MicroRabbit.Domain.Core.Bus;
using MicroRabbit.Domain.Core.Commands;
using MicroRabbit.Domain.Core.Events;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMq.Infra.Bus
{
    public sealed class RabbitMqBus : IEventBus
    {
        #region Private Readonly Properties
        private readonly IMediator _mediatr;
        private readonly List<Type> _eventTypes;
        private readonly Dictionary<string, List<Type>> _handlers;
        #endregion

        #region Constructors
        public RabbitMqBus(IMediator mediator)
        {
            _mediatr = mediator;
            _eventTypes = new List<Type>();
            _handlers = new Dictionary<string, List<Type>>();
        }
        #endregion

        #region Methods
        public Task SendCommand<T>(T command) where T : Command
        {
            return _mediatr.Send(command);
        }

        public void Publish<T>(T @event) where T : Event
        {
            ConnectionFactory factory = new ConnectionFactory { HostName = "localhost" };

            using (IConnection connection = factory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                string eventName = @event.GetType().Name;
                channel.QueueDeclare(eventName, false, false, false, null);

                string message = JsonConvert.SerializeObject(@event);
                byte[] body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish("", eventName, null, body);
            }
        }

        public void Subscribe<T, TH>()
            where T : Event
            where TH : IEventHandler<T>
        {
            string eventName = typeof(T).Name;
            Type handlerType = typeof(TH);

            if (!_eventTypes.Contains(typeof(T)))
            {
                _eventTypes.Add(typeof(T));
            }

            if (!_handlers.ContainsKey(eventName))
            {
                _handlers.Add(eventName, new List<Type>());
            }

            if (_handlers[eventName].Any(h => h.GetType() == handlerType))
            {
                throw new ArgumentException($"Handler type {handlerType.Name} already exist in event '{eventName}'", nameof(handlerType));
            }

            _handlers[eventName].Add(handlerType);

            StartBasicConsume<T>();
        }
        #endregion

        #region Private Methods
        private void StartBasicConsume<T>() where T : Event
        {
            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = "localhost",
                DispatchConsumersAsync = true
            };

            using (IConnection connection = factory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                string eventName = typeof(T).Name;

                channel.QueueDeclare(eventName, false, false, false, null);

                AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += Consumer_Received;

                channel.BasicConsume(eventName, true, consumer);
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            string eventName = @event.RoutingKey;
            string message = Encoding.UTF8.GetString(@event.Body.ToArray());

            try
            {
                await ProcessEvent(eventName, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
            }
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            if(_handlers.ContainsKey(eventName))
            {
                List<Type> subscriptions = _handlers[eventName];

                foreach (Type subscription in subscriptions)
                {
                    // creates an instance of every subscripton
                    object handler = Activator.CreateInstance(subscription);
                    if (handler == null) continue;

                    // find the eventType to process out of registered event types
                    Type eventType = _eventTypes.SingleOrDefault(t => t.Name.Equals(eventName));

                    // Deserialize the json to specified .Net type
                    object @event = JsonConvert.DeserializeObject(message, eventType);
                    
                    Type concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { @event });
                }
            }
        }
        #endregion
    }
}
