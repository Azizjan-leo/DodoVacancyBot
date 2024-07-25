using DAL;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Webhook.Controllers;
using Webhook.Controllers.Data;

var builder = WebApplication.CreateBuilder(args);

// Setup bot configuration
var botConfigSection = builder.Configuration.GetSection("BotConfiguration");

builder.Services.Configure<BotConfiguration>(botConfigSection);
builder.Services.AddHttpClient("tgwebhook").RemoveAllLoggers().AddTypedClient<ITelegramBotClient>(
    httpClient => new TelegramBotClient(botConfigSection.Get<BotConfiguration>()!.BotToken, httpClient));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<Webhook.Controllers.Services.UpdateHandler>();
builder.Services.AddScoped<Repository>();

builder.Services.ConfigureTelegramBotMvc();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
