using Microsoft.Extensions.Options;
using Telegram.Bot;
using Console.Advanced;
using Console.Advanced.Services;
using Microsoft.EntityFrameworkCore;
using Console.Advanced.Data;

var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!;

IHost host = Host.CreateDefaultBuilder(args)
    .UseEnvironment(environmentName)
    .ConfigureServices((context, services) =>
    {
        // Register Bot configuration
        services.Configure<BotConfiguration>(context.Configuration.GetSection("BotConfiguration"));
        
        services.AddHttpClient("telegram_bot_client").RemoveAllLoggers()
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    BotConfiguration? botConfiguration = sp.GetService<IOptions<BotConfiguration>>()?.Value;
                    ArgumentNullException.ThrowIfNull(botConfiguration);
                    TelegramBotClientOptions options = new(botConfiguration.BotToken);
                    return new TelegramBotClient(options, httpClient);
                });

        var connectionString = context.Configuration.GetConnectionString("DefaultConnection")!;

        services.AddDbContext<ApplicationDBContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<MyDbContextFactory>(provider =>
        {
            return new MyDbContextFactory(connectionString);
        });

        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
    })
    .Build();

await host.RunAsync();
