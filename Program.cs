using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Discord.Social.CSharp.Sdk;
using OpenCampfire.Options;
using OpenCampfire.Stores;
using OpenCampfire.Abstractions;
using OpenCampfire.Services;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile(
    "Configuration/discord-configuration.json",
    optional: false,
    reloadOnChange: false);

builder.Services.AddOptions<DiscordOptions>()
    .Bind(builder.Configuration)
    .Validate(options => options.ApplicationId != 0, "Discord ApplicationId must be configured.")
    .ValidateOnStart();

builder.Services.AddSingleton<DiscordClient>();
builder.Services.AddSingleton<IAuthorizationTokenStore, FileAuthorizationTokenStore>();
builder.Services.AddSingleton<IOpenCampfireService, OpenCampfireService>();

using IHost host = builder.Build();
IOpenCampfireService openCampfireService = host.Services.GetRequiredService<IOpenCampfireService>();

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