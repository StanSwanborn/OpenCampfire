# OpenCampfire.Localhost

`OpenCampfire.Localhost` provides the local web interface for OpenCampfire.

It hosts the OpenCampfire web UI, HTTP endpoints, and webhook logic, exposing the functionality provided by `OpenCampfire.Core` through a local web application.

The application is intended to be available at:

```text
http://opencampfire.localhost/
```

## Localhost

The `OpenCampfire.Localhost` project is the web-facing layer of OpenCampfire. It provides a local HTTP server around the functionality in `OpenCampfire.Core`, allowing the application to expose browser-based UI and HTTP/webhook endpoints.

The development configuration uses port **80**, giving OpenCampfire a clean local URL without requiring users to specify a port:

```text
http://opencampfire.localhost/
```

ASP.NET Core uses Kestrel as its cross-platform web server and supports configuring the application to listen on port 80.

### Linux

On Linux, ports below 1024 are privileged. Therefore, the OpenCampfire executable needs the `CAP_NET_BIND_SERVICE` capability to bind to port 80 without running the application itself as root.

A setup script is provided for this purpose:

```bash
./scripts/setup-port80-linux.sh
```

The script:

1. Locates `OpenCampfire.Localhost.csproj` relative to the script.
2. Builds the project.
3. Locates the generated `OpenCampfire.Localhost` executable.
4. Uses `sudo` to grant it `CAP_NET_BIND_SERVICE`.
5. Leaves the application itself running as the normal user.

Run the script once after cloning the repository:

```bash
./scripts/setup-port80-linux.sh
```

You will be prompted for your administrator password when the capability is applied.

Afterwards, the application can be started normally:

```bash
dotnet run --no-build
```

**Do not run `dotnet run` with `sudo`.** The setup script only elevates the permission required to bind port 80; it does not make the OpenCampfire application run as root.

If the project is rebuilt and the generated executable is replaced, the capability may need to be applied again by rerunning the setup script.

### Windows

No Linux-specific port setup is required on Windows. The project can be started normally:

```bash
dotnet run
```

## URL

Once running, the application is available at:

```text
http://opencampfire.localhost/
```

For example, the stream watcher is exposed through:

```text
http://opencampfire.localhost/stream/watch?ticket={ticket}
```
