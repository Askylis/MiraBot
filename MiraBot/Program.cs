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
using MiraBot.Miraminders;
using MiraBot.Permissions;
using Fergun.Interactive;
using MiraBot.Modules;
using MiraBot.Common;
using Discord.Commands;


var builder = Host.CreateApplicationBuilder(args);

var configBuilder = new ConfigurationBuilder();
configBuilder.AddEnvironmentVariables();
configBuilder.AddJsonFile("appsettings.json");
configBuilder.AddUserSecrets<Program>();
var config = configBuilder.Build();
builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton<CommandService>();
builder.Services.AddSingleton(x =>
{
    var discordSocketClient = x.GetRequiredService<DiscordSocketClient>();
    return new InteractionService(discordSocketClient);
});
builder.Services.AddSingleton(new InteractiveConfig { DefaultTimeout = TimeSpan.FromMinutes(5) });
builder.Services.AddSingleton<InteractiveService>();
builder.Services.AddSingleton<RemindersCache>();
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddHostedService<CommandHandlingService>();
builder.Services.AddHostedService<InteractionHandlingService>();
builder.Services.AddHostedService<DiscordStartupService>();
builder.Services.AddHostedService<RemindersProcessingService>();
builder.Services.AddHostedService<UsersRefreshService>();
builder.Services.Configure<DiscordOptions>(config.GetSection("Discord"));
builder.Services.Configure<DatabaseOptions>(config.GetSection("Database"));
builder.Services.Configure<ReminderOptions>(config.GetSection("Reminders"));
builder.Services.AddScoped<IGroceryAssistantRepository, GroceryAssistantRepository>();
builder.Services.AddScoped<IMiramindersRepository, MiramindersRepository>();
builder.Services.AddScoped<PermissionsRepository>();
builder.Services.AddScoped<UsersRepository>();
builder.Services.AddSingleton<IRemindersCache, RemindersCache>();
builder.Services.AddSingleton<UsersCache>();
builder.Services.AddTransient<GroceryAssistantComponents>();
builder.Services.AddTransient<GroceryAssistant>();
builder.Services.AddTransient<PermissionsHandler>();
builder.Services.AddTransient<MiraminderService>();
builder.Services.AddTransient<ReminderHandler>();
builder.Services.AddTransient<ModuleHelpers>();

using (var host = builder.Build())
{
    await host.RunAsync();
}