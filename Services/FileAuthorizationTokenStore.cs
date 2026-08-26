using System.Text.Json;
using Microsoft.Extensions.Options;

public sealed class FileAuthorizationTokenStore : IAuthorizationTokenStore
{
    private readonly string _filePath;

    public FileAuthorizationTokenStore(IOptions<DiscordOptions> options)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenCampfire");
        _filePath = Path.Combine(directory, $"discord-{options.Value.ApplicationId}.json");
    }

    public AuthorizationToken? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return null;

            return JsonSerializer.Deserialize<AuthorizationToken>(File.ReadAllText(_filePath));
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
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
        catch (FileNotFoundException)
        {
        }
    }

    private static void RestrictToCurrentUser(string path)
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}