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

public class GetTestObjectConsumer : BackgroundService, IConsumer
{
    private readonly TestObjectRmqConfig _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly EventingBasicConsumer _consumer;
    private const string QueueName = "get-single-test-object-requests";
    private const string RoutingKey = "get-single-test-object-route";
    private readonly IServiceScopeFactory _scopeFactory; 

    public GetTestObjectConsumer(IServiceScopeFactory scopeFactory, IOptions<TestObjectRmqConfig> config) 
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
        consumer.Received += async (model, ea) =>
        {
            Console.WriteLine($"Received request: {ea.BasicProperties.CorrelationId}");
            
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            string responseMessage;
            try
            {
                // Try to process the message
                responseMessage = await ProcessRequest(message);
            }
            catch (ValidationException e)
            {
                // Handle the validation exception and prepare an error message
                Console.WriteLine(e);
                responseMessage = e.Message;
            }
            catch (NullReferenceException e)
            {
                // Handle the null reference exception that is thrown if no object matches the provided id. 
                Console.WriteLine(e);
                responseMessage = "Null reference:" + string.Join(",", e.Message);
            }
            catch (Exception e)
            {
                // Handle other exceptions and prepare a generic error message
                Console.WriteLine(e);
                responseMessage = "An error occurred: " + e.Message;
            }
            
            // Send the response back
            var responseBody = Encoding.UTF8.GetBytes(responseMessage);
            
            var replyProperties = _channel.CreateBasicProperties();
            replyProperties.CorrelationId = ea.BasicProperties.CorrelationId;

            // Responding to the request by sending a message to the exclusive response queue.
            _channel.BasicPublish(
                exchange: "",
                routingKey: ea.BasicProperties.ReplyTo,
                basicProperties: replyProperties,
                body: responseBody
            );
            
            Console.WriteLine(responseMessage);

            //_channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(QueueName, autoAck: true, consumer: consumer);
        return Task.CompletedTask;
    }
    
    public async Task<string> ProcessRequest(string requestMessage)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var testObject = await dbContext.TestObjects
                .Include(t => t.SniffingPoints)
                .SingleOrDefaultAsync(t => t.Id == Guid.Parse(requestMessage));

            if (testObject == null)
            {
                throw new NullReferenceException("No object with the provided identifier was found.");
            }

            // Validate the test object
            await ValidateTestObject(testObject);

            // Serializing the testObject to be returned as a JsonObject to the response queue.
            var options = new JsonSerializerOptions { WriteIndented = true };
            var responseMessage = JsonSerializer.Serialize(testObject, options);

            return responseMessage;
        }
        catch (ValidationException e)
        {
            Console.WriteLine($"Validation failed: {e.Message}");
            throw; // Simply re-throw the original exception
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