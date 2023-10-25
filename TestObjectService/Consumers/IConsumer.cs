namespace TestObjectService.Consumers;

public interface IConsumer
{
    public Task StartListening();
    Task<string> ProcessRequest(string requestMessage);
}