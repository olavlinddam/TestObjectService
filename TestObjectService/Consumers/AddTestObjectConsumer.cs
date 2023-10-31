using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TestObjectService.Configurations;
using TestObjectService.Data;
using TestObjectService.Models;
using TestObjectService.Models.Validation;

namespace TestObjectService.Consumers;

public class AddTestObjectConsumer : BackgroundService, IConsumer
{
    private readonly TestObjectRmqConfig _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly EventingBasicConsumer _consumer;
    private const string QueueName = "add-single-test-object-requests";
    private const string RoutingKey = "add-single-test-object-route";
    private readonly IServiceScopeFactory _scopeFactory; 

    public AddTestObjectConsumer(IServiceScopeFactory scopeFactory, IOptions<TestObjectRmqConfig> config) 
    {
        _scopeFactory = scopeFactory;
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
        
        _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
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
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("AddTestObjectConsumer is stopping.");
        Dispose(); // Ensure resources are disposed
        await base.StopAsync(stoppingToken);
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
            _channel.BasicConsume(QueueName, false, consumer); 
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
            // Serialize the request to a TestObject object
            using var doc = JsonDocument.Parse(requestMessage);
            var formattedDoc= doc.RootElement.ToString();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            var testObject = JsonSerializer.Deserialize<TestObject>(formattedDoc, options);
            if (testObject == null) throw new NullReferenceException("Test object was null");
            
            // Add the necessary ids. 
            testObject.Id = Guid.NewGuid();
            foreach (var point in testObject.SniffingPoints)
            {
                point.Id = Guid.NewGuid();
                point.TestObjectId = testObject.Id;
            }


            // Validate the test object
            await ValidateTestObject(testObject);

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.AddAsync(testObject);
            await dbContext.SaveChangesAsync();

            return testObject.Id.ToString();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }

    private static async Task ValidateTestObject(TestObject testObject)
    {
        try
        {
            var testObjectValidator = new TestObjectValidator();
            var validationErrors = new List<string>();
            
            var validationResult = await testObjectValidator.ValidateAsync(testObject);
            if (!validationResult.IsValid)
            {
                validationErrors.AddRange(validationResult.Errors.Select(e => e.ErrorMessage));
            }
            if (validationErrors.Any())
            {
                throw new ValidationException(
                    $"Some LeakTest objects could not be validated: {string.Join(", ", validationErrors)}");
            }
        }
        catch (ValidationException e)
        {
            Console.WriteLine(e);
            throw;
        }

            


    }
}