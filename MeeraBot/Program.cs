using Discord.Interactions;
using Discord.WebSocket;
using MeeraBot.Options;
using MeeraBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var builder = Host.CreateApplicationBuilder(args);

var configBuilder = new ConfigurationBuilder();
configBuilder.AddEnvironmentVariables();
configBuilder.AddJsonFile("appsettings.json");
configBuilder.AddUserSecrets<Program>();
var config = configBuilder.Build();

builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton<InteractionService>();
builder.Services.AddHostedService<InteractionHandlingService>();
builder.Services.AddHostedService<DiscordStartupService>();
builder.Services.Configure<DiscordOptions>(config.GetSection("Discord"));

using (var host = builder.Build())
{
    await host.RunAsync();
}