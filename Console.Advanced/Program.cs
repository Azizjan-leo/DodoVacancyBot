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

        System.Console.WriteLine("BOT TOKEN: " + 
            context.Configuration.GetSection("BotConfiguration").GetValue<string>("BotToken"));
 
        // Register named HttpClient to benefits from IHttpClientFactory
        // and consume it with ITelegramBotClient typed client.
        // More read:
        //  https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0#typed-clients
        //  https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
        services.AddHttpClient("telegram_bot_client").RemoveAllLoggers()
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    BotConfiguration? botConfiguration = sp.GetService<IOptions<BotConfiguration>>()?.Value;
                    ArgumentNullException.ThrowIfNull(botConfiguration);
                    TelegramBotClientOptions options = new(botConfiguration.BotToken);
                    return new TelegramBotClient(options, httpClient);
                });

        services.AddDbContext<ApplicationContext>(options => 
            options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
    })
    .Build();

await host.RunAsync();
