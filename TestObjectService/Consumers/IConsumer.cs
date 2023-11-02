namespace TestObjectService.Consumers;

public interface IConsumer<TResponse>
{
    public Task StartListening();
    Task<TResponse> ProcessRequest(string requestMessage);
}