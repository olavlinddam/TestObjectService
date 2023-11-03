using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using TestObjectService.Configurations;
using TestObjectService.Consumers;
using TestObjectService.Data;
using Microsoft.EntityFrameworkCore;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppSettings() // Use the custom configuration setup
    .ConfigureServices((context, services) =>
    {
        services.Configure<TestObjectRmqConfig>(context.Configuration.GetSection("TestObjectRmqConfig"));
        services.AddHostedService<AddTestObjectConsumer>();
        services.AddHostedService<GetTestObjectConsumer>();
        services.AddHostedService<UpdateTestObjectConsumer>();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));
    })
    .Build();

PrintConfiguration(host.Services);

host.Run();

void PrintConfiguration(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    foreach (var section in config.AsEnumerable())
    {
        Console.WriteLine($"{section.Key} = {section.Value}");
    }
}