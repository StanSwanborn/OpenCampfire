using Discord.Social.CSharp.Sdk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
builder.Services.AddSingleton<IOpenCampfireService, OpenCampfireService>();

using IHost host = builder.Build();
IOpenCampfireService openCampfireService = host.Services.GetRequiredService<IOpenCampfireService>();

await openCampfireService.AuthorizeAsync();

while (true)
{
    await openCampfireService.RunCallbacksAsync().ConfigureAwait(false);
    await Task.Delay(10);
}