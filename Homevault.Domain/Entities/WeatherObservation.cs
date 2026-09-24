namespace Homevault.Domain.Entities;

public sealed class WeatherObservation
{
    private WeatherObservation()
    {
    }

    public WeatherObservation(
        string city,
        string state,
        decimal latitude,
        decimal longitude,
        decimal temperatureCelsius,
        decimal relativeHumidity,
        DateTimeOffset observedAt,
        DateTimeOffset collectedAt)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("A cidade é obrigatória.", nameof(city));
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("O estado é obrigatório.", nameof(state));
        }

        if (relativeHumidity is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(relativeHumidity),
                "A umidade relativa deve estar entre 0 e 100.");
        }

        Id = Guid.NewGuid();
        City = city.Trim();
        State = state.Trim();
        Latitude = latitude;
        Longitude = longitude;
        TemperatureCelsius = temperatureCelsius;
        RelativeHumidity = relativeHumidity;
        ObservedAt = observedAt;
        CollectedAt = collectedAt;
    }

    public Guid Id { get; private set; }

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    public decimal Latitude { get; private set; }

    public decimal Longitude { get; private set; }

    public decimal TemperatureCelsius { get; private set; }

    public decimal RelativeHumidity { get; private set; }

    public DateTimeOffset ObservedAt { get; private set; }

    public DateTimeOffset CollectedAt { get; private set; }
}
