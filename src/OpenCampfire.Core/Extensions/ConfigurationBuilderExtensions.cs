using Microsoft.Extensions.Configuration;

namespace OpenCampfire.Core.Extensions;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddOpenCampfireConfiguration(
        this IConfigurationBuilder configuration)
    {
        var assembly = typeof(ConfigurationBuilderExtensions).Assembly;

        var stream = assembly.GetManifestResourceStream(
            "OpenCampfire.Core.Configuration.opencampfire-configuration.json"
        ) ?? throw new InvalidOperationException("OpenCampfire configuration resource was not found.");

        configuration.AddJsonStream(stream);

        return configuration;
    }
}