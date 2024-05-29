using System.Text.Json;
using System.Text;
using RabbitMQ.Client;

namespace SımpleBankingApp.Web.Services
{
    public class RabbitMQService
    {
        private readonly IConnectionFactory _factory;

        public RabbitMQService(IConnectionFactory factory)
        {
            _factory = factory;
        }

        public void PublishToQueue(string queueName, object data)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var message = JsonSerializer.Serialize(data);
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: queueName,
                                     basicProperties: null,
                                     body: body);
            }
        }
    }
}
