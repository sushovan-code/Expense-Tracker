using Microsoft.Extensions.Configuration.Json;

namespace ExpenseTracker.Api.Configuration;

public static class ConfigsFolder
{
    /// <summary>Folder (relative to the content root) that holds appsettings.json and appsettings.{Environment}.json.</summary>
    public const string Name = "configs";

    /// <summary>
    /// Points the host's default <c>appsettings.json</c> and <c>appsettings.{Environment}.json</c> sources at
    /// the <c>configs</c> folder. The sources are replaced in place, so precedence is unchanged:
    /// base file &lt; environment file &lt; user secrets &lt; environment variables &lt; command line.
    /// </summary>
    public static ConfigurationManager UseConfigsFolder(this ConfigurationManager configuration, IHostEnvironment environment)
    {
        const string baseFile = "appsettings.json";
        var environmentFile = $"appsettings.{environment.EnvironmentName}.json";
        var sources = configuration.Sources;

        for (var i = 0; i < sources.Count; i++)
        {
            if (sources[i] is not JsonConfigurationSource { Path: { } path } json) continue;

            var isBase = string.Equals(path, baseFile, StringComparison.OrdinalIgnoreCase);
            var isEnvironment = string.Equals(path, environmentFile, StringComparison.OrdinalIgnoreCase);
            if (!isBase && !isEnvironment) continue;

            sources[i] = new JsonConfigurationSource
            {
                FileProvider = json.FileProvider,
                Path = System.IO.Path.Combine(Name, path),
                // The shared base file must exist; an environment may legitimately have no overrides.
                Optional = !isBase,
                ReloadOnChange = json.ReloadOnChange,
            };
        }

        return configuration;
    }
}
