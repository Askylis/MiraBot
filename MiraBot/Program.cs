using Discord.Interactions;
using Discord.WebSocket;
using MiraBot.Options;
using MiraBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using MiraBot.GroceryAssistance;
using Fergun.Interactive;
using MiraBot.Modules;


var builder = Host.CreateApplicationBuilder(args);

var configBuilder = new ConfigurationBuilder();
configBuilder.AddEnvironmentVariables();
configBuilder.AddJsonFile("appsettings.json", true);
configBuilder.AddUserSecrets<Program>();
var config = configBuilder.Build();

builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton(x =>
{
    var discordSocketClient = x.GetRequiredService<DiscordSocketClient>();
    return new InteractionService(discordSocketClient);
});
builder.Services.AddSingleton(new InteractiveConfig { DefaultTimeout = TimeSpan.FromMinutes(5) });
builder.Services.AddSingleton<InteractiveService>();
builder.Services.AddHostedService<InteractionHandlingService>();
builder.Services.AddHostedService<DiscordStartupService>();
builder.Services.Configure<DiscordOptions>(config.GetSection("Discord"));
builder.Services.Configure<DatabaseOptions>(config.GetSection("Database"));
builder.Services.AddTransient<IGroceryAssistantRepository, GroceryAssistantRepository>();
builder.Services.AddTransient<GroceryAssistantComponents>();
builder.Services.AddTransient<GroceryAssistant>();

using (var host = builder.Build())
{
    await host.RunAsync();
}