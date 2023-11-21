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
using TestObjectService.Models.DTOs;
using TestObjectService.Models.Validation;

namespace TestObjectService.Consumers;

public class UpdateTestObjectConsumer : BackgroundService, IConsumer<string>
{
    private readonly TestObjectRmqConfig _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly EventingBasicConsumer _consumer;
    private const string QueueName = "update-single-test-object-requests";
    private const string RoutingKey = "update-single-test-object-route";
    private readonly IServiceScopeFactory _scopeFactory; 

    public UpdateTestObjectConsumer(IServiceScopeFactory scopeFactory, IOptions<TestObjectRmqConfig> config) 
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

            var responseMessage = await ProcessRequest(message);
            
            
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
            // Serialize the request to a TestObject object
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            Console.WriteLine("Parsing to TestObject from json");
            var modifiedTestObject = JsonSerializer.Deserialize<TestObject>(requestMessage, options);
            if (modifiedTestObject == null) throw new NullReferenceException("Test object was null");

            Console.WriteLine(modifiedTestObject.Id);
            Console.WriteLine(requestMessage);
            // Validate the test object
            await ValidateTestObject(modifiedTestObject);

            // Pull the corresponding record from the db
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var testObjectToUpdate =
                await dbContext.TestObjects.Include(x => x.SniffingPoints)
                    .Where(t => t.Id == modifiedTestObject.Id).SingleOrDefaultAsync();

            // Check if existing object is null and return an error if it is.
            if (testObjectToUpdate == null)
            {
                Console.WriteLine("no object matched the id in the database");
                throw new NullReferenceException("The provided id does not match an existing record.");
            }

            Console.WriteLine("Updating Testobject");
            // Map the updated properties to the retrieved entity
            dbContext.Entry(testObjectToUpdate).CurrentValues.SetValues(modifiedTestObject);
            
            Console.WriteLine("Updating related sniffing points");
            
            // Existing SniffingPoints from the database
            var existingSniffingPoints = testObjectToUpdate.SniffingPoints.ToList();

            foreach (var sniffingPoint in modifiedTestObject.SniffingPoints)
            {
                var existingSniffingPoint = existingSniffingPoints
                    .FirstOrDefault(sp => sp.Id == sniffingPoint.Id);

                if (existingSniffingPoint != null)
                {
                    // Update existing sniffing point
                    dbContext.Entry(existingSniffingPoint).CurrentValues.SetValues(sniffingPoint);
                }
                else
                {

                    var newSniffingPoint = new SniffingPoint();
                    newSniffingPoint.Id = Guid.NewGuid();
                    newSniffingPoint.TestObjectId = testObjectToUpdate.Id;
                    newSniffingPoint.Name = sniffingPoint.Name
                    newSniffingPoint.X = sniffingPoint.X
                    newSniffingPoint.Y = sniffingPoint.Y
                    // Add new sniffing point
                    testObjectToUpdate.SniffingPoints.Add(newSniffingPoint);
                }
            }

            // Handle deleted sniffing points
            foreach (var existingSniffingPoint in existingSniffingPoints)
            {
                if (modifiedTestObject.SniffingPoints.All(sp => sp.Id != existingSniffingPoint.Id))
                {
                    // If a sniffing point in the database does not exist in the modified test object, remove it
                    dbContext.Remove(existingSniffingPoint);
                }
            }

            await dbContext.SaveChangesAsync();


            // return the updated record
            return CreateApiResponse(200, testObjectToUpdate, null);
        }
        catch (DbUpdateConcurrencyException e)
        {
            Console.WriteLine(e.Message);
            return CreateApiResponse(409, null, $"Concurrency error: {e.Message}");
        }
        catch (DbUpdateException e)
        {
            Console.WriteLine(e.Message);
            return CreateApiResponse(500, null, $"An internal error occured. Please try again later.");
        }
        catch (NullReferenceException e)
        {
            Console.WriteLine(e.Message);
            return CreateApiResponse(400, null, e.Message);
        }
        catch (ValidationException e)
        {
            Console.WriteLine($"Validation failed: {e.Message}");
            return CreateApiResponse(400, null, e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
            return CreateApiResponse(500, null, e.Message);
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
                    $"The test object could not be validated: {string.Join(", ", validationErrors)}");
            }
        }
        catch (ValidationException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    private static string CreateApiResponse(int statusCode, TestObject data, string errorMessage)
    {
        var apiResponse = new ApiResponse<TestObject>
        {
            StatusCode = statusCode,
            Data = data,
            ErrorMessage = errorMessage
        };

        return JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions { WriteIndented = true });
    }
}