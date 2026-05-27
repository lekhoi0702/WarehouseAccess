namespace WarehouseAccessAPI.Configurations;

public class ExternalGuestApiOptions
{
    public const string SectionName = "ExternalGuestApi";
    public string BaseUrl { get; set; } = "http://192.168.0.38:8000";
    public int TimeoutSeconds { get; set; } = 8;
}

