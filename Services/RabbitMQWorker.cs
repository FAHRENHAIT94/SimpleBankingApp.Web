using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace SımpleBankingApp.Web.Services
{
    public class RabbitMQWorker : BackgroundService
    {
        private readonly IConnectionFactory _factory;

        public RabbitMQWorker(IConnectionFactory factory)
        {
            _factory = factory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "your-queue-name",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var data = JsonSerializer.Deserialize<object>(message);

                    // Gelen mesajı işleyin
                };

                channel.BasicConsume(queue: "your-queue-name",
                                     autoAck: true,
                                     consumer: consumer);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
