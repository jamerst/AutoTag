using System.Reflection;

namespace AutoTag.CLI;

public static class ApiKeys
{
    public static string? TMDBKey
    {
        get
        {
            var envKey = Normalise(Environment.GetEnvironmentVariable("TMDB_API_KEY"));
            if (envKey != null)
            {
                return envKey;
            }

            // Support the repo's older optional Keys.cs override without requiring it for builds.
            var legacyKeysType = Assembly.GetExecutingAssembly().GetType("AutoTag.CLI.Keys");
            var legacyKey = legacyKeysType?
                .GetProperty("TMDBKey", BindingFlags.Public | BindingFlags.Static)?
                .GetValue(null) as string;

            return Normalise(legacyKey);
        }
    }

    private static string? Normalise(string? value)
        => string.IsNullOrWhiteSpace(value) || value == "TMDB_API_KEY"
            ? null
            : value;
}
