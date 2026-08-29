using OpenCampfire.Localhost;
using System.Net.Sockets;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(
    "http://opencampfire.localhost:80");

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
    Console.Error.WriteLine();
    Console.Error.WriteLine("============================================================");
    Console.Error.WriteLine(" OpenCampfire could not bind to port 80.");
    Console.Error.WriteLine("============================================================");
    Console.Error.WriteLine();
    Console.Error.WriteLine("On Linux, binding to port 80 requires additional permission.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Please run the following command once:");
    Console.Error.WriteLine();
    Console.Error.WriteLine("    ./scripts/setup-port80-linux.sh");
    Console.Error.WriteLine();
    Console.Error.WriteLine("You will be asked for your administrator password.");
    Console.Error.WriteLine("Afterwards, you can start OpenCampfire normally without sudo.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("============================================================");
    Console.Error.WriteLine();

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