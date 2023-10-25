namespace TestObjectService.Consumers;

public class AddTestObjectConsumer : BackgroundService
{
    public AddTestObjectConsumer()
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}