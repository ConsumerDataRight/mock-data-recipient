using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;
using System;
using CDR.DiscoverDataHolders;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
            //while running on local machine via vs studio these are settings used
            .AddJsonFile("local.settings.json", true, true)
            //while running docker these are config values used
            .AddJsonFile("appsettings.docker.json", true, true)
            .AddEnvironmentVariables()
            .AddCommandLine(Environment.GetCommandLineArgs())
            .Build();

        if (context.HostingEnvironment.IsDevelopment() && !string.IsNullOrEmpty(context.HostingEnvironment.ApplicationName))
        {
            Console.WriteLine("Development environment");
            builder.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
        }
    })
    .ConfigureServices(services =>
    {
        services.AddOptions<DHOptions>()
        .Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.Bind(settings);
        });

        services.AddTransient<DHHttpClientHandler, DHHttpClientHandler>();
        services.AddHttpClient(DiscoverDHConstants.DHHttpClientName, (provider, client) =>
        {

        }).ConfigurePrimaryHttpMessageHandler<DHHttpClientHandler>();

    })
    .Build();

host.Run();
