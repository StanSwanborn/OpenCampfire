using OpenCampfire.Abstractions.Interfaces;
using Microsoft.Extensions.Options;
using OpenCampfire.Authorization;
using OpenCampfire.Options;
using System.Text.Json;

namespace OpenCampfire.Stores;

public sealed class FileAuthorizationTokenStore : IAuthorizationTokenStore
{
    private readonly string _filePath;
    private AuthorizationToken? deserializedToken;

    public FileAuthorizationTokenStore(IOptions<DiscordOptions> options)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenCampfire"
        );

        _filePath = Path.Combine(directory, $"discord-{options.Value.ApplicationId}.json");
    }

    public AuthorizationToken? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return null;

            if(deserializedToken == null)
                deserializedToken = JsonSerializer.Deserialize<AuthorizationToken>(File.ReadAllText(_filePath));

            return deserializedToken;
        }
        catch (IOException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw; // TODO: Evaluate necessity, or maybe we should just throw a custom exception.
        }
    }

    public void Save(AuthorizationToken token)
    {
        var directory = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(token));
        RestrictToCurrentUser(temporaryPath);
        File.Move(temporaryPath, _filePath, overwrite: true);
        RestrictToCurrentUser(_filePath);
    }

    public void Clear()
    {
        try
        {
            File.Delete(_filePath);
        }
        catch (FileNotFoundException) // If the file doesn't exist, we can consider it already cleared.
        {
        }
    }

    private static void RestrictToCurrentUser(string path)
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}