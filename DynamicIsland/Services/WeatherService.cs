using System.Net.Http;
using System.Text.Json;

namespace DynamicIsland.Services;

public class WeatherData
{
    public double Temperature { get; set; }
    public int WeatherCode { get; set; }
    public string Description { get; set; } = "Ясно";
    public string Icon { get; set; } = "☀️";
}

public class WeatherService
{
    private static readonly Lazy<WeatherService> _instance = new(() => new WeatherService());
    public static WeatherService Instance => _instance.Value;

    private readonly HttpClient _http = new();
    private WeatherData? _cachedData;
    private DateTime _lastFetch = DateTime.MinValue;

    public WeatherData? CurrentWeather => _cachedData;
    public event Action<WeatherData>? WeatherUpdated;

    private WeatherService()
    {
        _ = RefreshWeatherAsync();
    }

    public async Task RefreshWeatherAsync(bool force = false)
    {
        if (!force && _cachedData != null && (DateTime.UtcNow - _lastFetch).TotalMinutes < 30)
            return;

        try
        {
            var config = ConfigService.Instance.Config;
            double lat = config.WeatherLatitude;
            double lon = config.WeatherLongitude;

            string url = $"https://api.open-meteo.com/v1/forecast?latitude={lat:0.0000}&longitude={lon:0.0000}&current_weather=true";
            var response = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var current = doc.RootElement.GetProperty("current_weather");

            double temp = current.GetProperty("temperature").GetDouble();
            int code = current.GetProperty("weathercode").GetInt32();

            var (desc, icon) = MapWeatherCode(code);

            _cachedData = new WeatherData
            {
                Temperature = temp,
                WeatherCode = code,
                Description = desc,
                Icon = icon
            };
            _lastFetch = DateTime.UtcNow;

            WeatherUpdated?.Invoke(_cachedData);
        }
        catch
        {
            // fallback
        }
    }

    private static (string Desc, string Icon) MapWeatherCode(int code) => code switch
    {
        0 => ("Ясно", "☀️"),
        1 or 2 => ("Переменная облачность", "⛅"),
        3 => ("Пасмурно", "☁️"),
        45 or 48 => ("Туман", "🌫️"),
        51 or 53 or 55 => ("Морось", "🌦️"),
        61 or 63 or 65 => ("Дождь", "🌧️"),
        71 or 73 or 75 => ("Снег", "❄️"),
        80 or 81 or 82 => ("Ливень", "🌧️"),
        95 or 96 or 99 => ("Гроза", "⛈️"),
        _ => ("Облачно", "⛅")
    };
}
