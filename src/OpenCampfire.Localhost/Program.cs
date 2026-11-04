using OpenCampfire.Localhost;
using OpenCampfire.Core.Extensions;
using OpenCampfire.Options;
using System.Net.Sockets;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddOpenCampfireConfiguration();

LocalhostOptions localhostOptions = builder.Configuration
    .GetSection("Localhost")
    .Get<LocalhostOptions>()
    ?? throw new InvalidOperationException("OpenCampfire Localhost configuration is missing.");

builder.WebHost.UseUrls(
    $"http://opencampfire.localhost:{localhostOptions.Port}"
);

WebApplication app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapWebApi();

try
{
    await app.StartAsync();
    await app.WaitForShutdownAsync();
}
catch (IOException ex) when (IsPermissionDenied(ex))
{
    Console.WriteLine($"Permission to bind to the requested application port of {localhostOptions.Port} was denied by the Operating System.");
    Console.WriteLine("Please ensure that other applications that have claimed this port are closed.");

    Console.WriteLine("=== CRITICAL FAILURE /portbinding ===");
    Environment.Exit(1);
}    

static bool IsPermissionDenied(Exception exception)
{
    if (exception is SocketException
    {
        SocketErrorCode: SocketError.AccessDenied
    })
    {
        return true;
    }

    return exception.InnerException is not null
        && IsPermissionDenied(exception.InnerException);
}