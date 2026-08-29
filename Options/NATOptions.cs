namespace OpenCampfire.Options;

public record NATOptions
{
    public required IReadOnlyDictionary<string, string> NATServers { get; init; }
}