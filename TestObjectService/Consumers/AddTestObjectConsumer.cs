using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TestObjectService.Configurations;


namespace TestObjectService.Consumers;

public class AddTestObjectConsumer : BackgroundService, IConsumer
{
    private readonly TestObjectRmqConfig _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly EventingBasicConsumer _consumer;
    private const string QueueName = "add-single-requests";
    private const string RoutingKey = "add-single-route";
    
    public AddTestObjectConsumer(IOptions<TestObjectRmqConfig> config)
    {
        _config = config.Value;
        
        var factory = new ConnectionFactory
        {
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost,  
            HostName = _config.HostName,
            Port = int.Parse(_config.Port),
            ClientProvidedName = _config.ClientProvidedName
        };
        
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Direct, durable: true);
        
        _channel.QueueDeclare(QueueName, exclusive: false);
        _channel.QueueBind(QueueName, _config.ExchangeName, RoutingKey);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Console.WriteLine("AddTestObjectConsumer started listening.");
            await StartListening();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"An error occured: {e.Message}");
        }


        // Keep the service running until it's stopped or cancelled
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            Dispose();
        }
    }

    public Task StartListening()
    {
        var consumer = new EventingBasicConsumer(_channel);
        try
        {
            consumer.Received += async (model, ea) =>
            {
                Console.WriteLine($"Received request: {ea.BasicProperties.CorrelationId}");
            
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                // Process the message
                var responseMessage = await ProcessRequest(message);

                // Send the response back
                var responseBody = Encoding.UTF8.GetBytes(responseMessage);
            
                _channel.BasicPublish(
                    exchange: "",
                    routingKey: ea.BasicProperties.ReplyTo,
                    basicProperties: null, // potentielt skal vi sende corr id med.
                    body: responseBody
                );
            
                Console.WriteLine(responseMessage);
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };
        }
        catch (Exception e)
        {
            // log exception here
            Console.WriteLine(e);
            throw new Exception(e.Message);
        }
        
        return Task.CompletedTask;
    }
    
    public async Task<string> ProcessRequest(string requestMessage)
    {
        try
        {
            using var doc = JsonDocument.Parse(requestMessage);
            var formattedDoc= doc.RootElement.ToString();
        
            // Implementer logik her
            return "test";
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    private void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}