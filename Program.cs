using Microsoft.Extensions.DependencyInjection;
using OpenCampfire.Abstractions.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Discord.Social.CSharp.Sdk;
using OpenCampfire.Services;
using OpenCampfire.Options;
using OpenCampfire.Stores;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile(
    "Configuration/opencampfire-configuration.json",
    optional: false,
    reloadOnChange: false);

builder.Services
    .AddOptions<DiscordOptions>()
    .Bind(builder.Configuration.GetSection("Discord"));

builder.Services
    .AddOptions<NATOptions>()
    .Bind(builder.Configuration.GetSection("NAT"));

builder.Services.AddSingleton<DiscordClient>();
builder.Services.AddSingleton<IAuthorizationTokenStore, FileAuthorizationTokenStore>();
builder.Services.AddSingleton<IOpenCampfireDiscordService, OpenCampfireDiscordService>();

using IHost host = builder.Build();
IOpenCampfireDiscordService openCampfireService = host.Services.GetRequiredService<IOpenCampfireDiscordService>();

// OpenCampfire Commands
Dictionary<string, Func<Task>> commands = new()
{
    ["stream start"] = openCampfireService.StartStreamingAsync,
    ["exit"] = openCampfireService.DisposeAsync
};

await openCampfireService.AuthorizeAsync();
await openCampfireService.ConnectToDiscordAsync();

// Open thread for RunCallbacks pump.
Task callbackTask = Task.Run(async () =>
{
    while (true)
    {
        await openCampfireService.RunCallbacksAsync();
        await Task.Delay(10);
    }
});

// Open thread for user input and command execution.
Task inputTask = Task.Run(async () =>
{
    while (true)
    {
        string? input = await Console.In.ReadLineAsync();

        if (input is not null &&
            commands.TryGetValue(input, out Func<Task>? command))
        {
            await command();
        }
    }
});

await Task.WhenAll(callbackTask, inputTask);