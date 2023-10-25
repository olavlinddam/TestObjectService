using TestObjectService;
using TestObjectService.Configurations;
using TestObjectService.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<TestObjectRmqConfig>(context.Configuration.GetSection("TestObjectRmqConfig"));
        services.AddHostedService<AddTestObjectConsumer>();
    })
    .Build();

host.Run();