using TestObjectService;
using TestObjectService.Consumers;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<AddTestObjectConsumer>();
    })
    .Build();

host.Run();