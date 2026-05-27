using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WarehouseAccessAPI.Configurations;

namespace WarehouseAccessAPI.Services;

public interface IExternalGuestLookupService
{
    Task<(bool Success, ExternalGuestRecord? Record, string? Note)> TryLookupGuestAsync(
        string company,
        string userCode,
        string correlationId,
        CancellationToken cancellationToken = default);
}

public class ExternalGuestLookupService : IExternalGuestLookupService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalGuestLookupService> _logger;
    private readonly ExternalGuestApiOptions _options;

    public ExternalGuestLookupService(
        HttpClient httpClient,
        IOptions<ExternalGuestApiOptions> options,
        ILogger<ExternalGuestLookupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<(bool Success, ExternalGuestRecord? Record, string? Note)> TryLookupGuestAsync(
        string company,
        string userCode,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(userCode))
        {
            return (false, null, "Missing company or user code");
        }

        var tokenResult = await GetTokenWithRetryAsync(company, correlationId, cancellationToken);

        if (!tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.Token))
        {
            return (false, null, tokenResult.Note ?? "Token lookup failed");
        }

        var guestResult = await GetGuestWithRetryAsync(company, userCode, tokenResult.Token, correlationId, cancellationToken);

        if (!guestResult.Success)
        {
            return (false, null, guestResult.Note ?? "Guest lookup failed");
        }

        var selected = guestResult.Records?
            .FirstOrDefault(x => string.Equals(x.Cardnum?.Trim(), userCode.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? guestResult.Records?.FirstOrDefault();

        if (selected is null)
        {
            return (false, null, "No guest data found");
        }

        return (true, selected, null);
    }

    private async Task<(bool Success, string? Token, string? Note)> GetTokenAsync(
        string company,
        CancellationToken cancellationToken)
    {
        var path = $"/token?company={Uri.EscapeDataString(company)}";
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return (false, null, $"Token API HTTP {(int)response.StatusCode}");
        }

        var raw = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (false, null, "Token API returned empty body");
        }

        try
        {
            if (raw.StartsWith("{"))
            {
                var tokenObj = JsonSerializer.Deserialize<TokenResponse>(raw, JsonOptions);
                var token = tokenObj?.Token?.Trim();
                return string.IsNullOrWhiteSpace(token)
                    ? (false, null, "Token field is empty")
                    : (true, token, null);
            }

            return (true, raw.Trim('"'), null);
        }
        catch
        {
            return (true, raw.Trim('"'), null);
        }
    }

    private async Task<(bool Success, List<ExternalGuestRecord>? Records, string? Note)> GetGuestAsync(
        string company,
        string userCode,
        string token,
        CancellationToken cancellationToken)
    {
        var path = $"/api/v1/guest/list?com={Uri.EscapeDataString(company)}&limit=10&cardnum={Uri.EscapeDataString(userCode)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return (false, null, $"Guest API HTTP {(int)response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return (true, new List<ExternalGuestRecord>(), "Guest API returned empty body");
        }

        var records = JsonSerializer.Deserialize<List<ExternalGuestRecord>>(content, JsonOptions) ?? new List<ExternalGuestRecord>();
        return (true, records, null);
    }

    private async Task<(bool Success, string? Token, string? Note)> GetTokenWithRetryAsync(
        string company,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var first = await GetTokenAsync(company, cancellationToken);
        if (first.Success)
        {
            return first;
        }

        _logger.LogWarning("ExternalGuestLookup GetToken failed first attempt. CorrelationId={CorrelationId}; Note={Note}", correlationId, first.Note);
        var second = await GetTokenAsync(company, cancellationToken);
        if (!second.Success)
        {
            _logger.LogWarning("ExternalGuestLookup GetToken failed retry. CorrelationId={CorrelationId}; Note={Note}", correlationId, second.Note);
        }
        return second;
    }

    private async Task<(bool Success, List<ExternalGuestRecord>? Records, string? Note)> GetGuestWithRetryAsync(
        string company,
        string userCode,
        string token,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var first = await GetGuestAsync(company, userCode, token, cancellationToken);
        if (first.Success)
        {
            return first;
        }

        _logger.LogWarning("ExternalGuestLookup GetGuest failed first attempt. CorrelationId={CorrelationId}; Note={Note}", correlationId, first.Note);
        var second = await GetGuestAsync(company, userCode, token, cancellationToken);
        if (!second.Success)
        {
            _logger.LogWarning("ExternalGuestLookup GetGuest failed retry. CorrelationId={CorrelationId}; Note={Note}", correlationId, second.Note);
        }
        return second;
    }

    private sealed class TokenResponse
    {
        public string? Token { get; set; }
    }
}

public class ExternalGuestRecord
{
    public string? Company { get; set; }
    public string? Guestname { get; set; }
    public string? Guestno { get; set; }
    public string? Deptcontact { get; set; }
    public string? Cardnum { get; set; }
    public string? Cardtype { get; set; }
    public string? Purpose { get; set; }
}
