namespace Homevault.Infrastructure.Weather;

public sealed class WeatherOptions
{
    public const string SectionName = "Weather";

    public string ProviderBaseUrl { get; set; } = "https://api.open-meteo.com/v1/";

    public int RequestTimeoutSeconds { get; set; } = 10;

    public int CollectionIntervalMinutes { get; set; } = 15;

    public int RetentionMonths { get; set; } = 25;

    public LocationOptions Location { get; set; } = new();
}

public sealed class LocationOptions
{
    public string City { get; set; } = "Maringá";

    public string State { get; set; } = "PR";

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public string TimeZone { get; set; } = "America/Sao_Paulo";
}
