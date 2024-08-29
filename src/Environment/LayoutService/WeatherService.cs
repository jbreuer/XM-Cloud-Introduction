using System.Globalization;
using System.Text.Json;

namespace LayoutService;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public WeatherService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenWeatherMap:ApiKey"];
    }

    public async Task<string> GetCurrentWeatherAsync(string city)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.openweathermap.org/data/2.5/weather?q={city}&units=metric&appid={_apiKey}");
            response.EnsureSuccessStatusCode();

            var weatherData = await response.Content.ReadAsStringAsync();
            var weatherJson = JsonDocument.Parse(weatherData).RootElement;

            string temperature = weatherJson.GetProperty("main").GetProperty("temp").GetDecimal().ToString(CultureInfo.InvariantCulture);
            string description = weatherJson.GetProperty("weather")[0].GetProperty("description").GetString() ?? string.Empty;

            return $"Weather in {city}: {temperature}°C {description}";
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }
}