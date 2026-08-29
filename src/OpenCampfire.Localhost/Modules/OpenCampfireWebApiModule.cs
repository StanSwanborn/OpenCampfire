namespace OpenCampfire.Localhost;

public static class WebApiModule
{
    public static void MapWebApi(this WebApplication app)
    {
        app.MapGet(
            "/stream/watch",
            () => Results.File(
                Path.Combine(app.Environment.WebRootPath, "index.html"),
                "text/html"));

        app.MapGet(
            "/api/stream/resolve",
            (string ticket) =>
            {
                if (!TryParseTicket(
                        ticket,
                        out string endpoint,
                        out int port))
                {
                    return Results.BadRequest(new
                    {
                        error = "Invalid stream ticket."
                    });
                }

                return Results.Ok(new
                {
                    endpoint,
                    port
                });
            });
    }

    private static bool TryParseTicket(string ticket, out string endpoint, out int port)
    {
        endpoint = string.Empty;
        port = 0;

        int separator = ticket.LastIndexOf(':');

        if (separator <= 0 || separator == ticket.Length - 1)
        {
            return false;
        }

        endpoint = ticket[..separator];

        return int.TryParse(ticket[(separator + 1)..], out port);
    }
}