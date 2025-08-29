using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class ExternalAuthService : IExternalAuthService
{
	private readonly HttpClient _httpClient;
	private readonly string _authenticateUrl;
	private readonly ILogger<ExternalAuthService> _logger;

	public ExternalAuthService(HttpClient httpClient, IConfiguration configuration, ILogger<ExternalAuthService> logger)
	{
		_httpClient = httpClient;
		_logger = logger;
		var baseUrl = configuration["ExternalAuth:BaseUrl"]?.TrimEnd('/') ?? "";
		var path = configuration["ExternalAuth:AuthenticatePath"]?.Trim('/') ?? "User/api/IntermediariesAuthenticate";
		_authenticateUrl = string.IsNullOrWhiteSpace(baseUrl) ? path : $"{baseUrl}/{path}";

		// Ensure predictable request settings
		_httpClient.Timeout = TimeSpan.FromSeconds(25);
		_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
	}

	public async Task<ExternalAuthResult?> AuthenticateAsync(string phoneNumber, string password, CancellationToken cancellationToken = default)
	{
		var payload = new { userName = phoneNumber, password };
		try
		{
			var target = new Uri(_authenticateUrl);
			Uri? proxyUri = null;
			try { proxyUri = System.Net.WebRequest.DefaultWebProxy?.GetProxy(target); } catch {}
			_logger.LogInformation("Calling external auth at {Url} for phone {Phone}; proxy={Proxy}", _authenticateUrl, phoneNumber, proxyUri?.ToString() ?? "none");
			using var response = await _httpClient.PostAsJsonAsync(_authenticateUrl, payload, cancellationToken);
			var body = await response.Content.ReadAsStringAsync(cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("External auth failed with status {Status}: {Body}", (int)response.StatusCode, Truncate(body));
				return null;
			}

			using var doc = JsonDocument.Parse(body);
			var root = doc.RootElement;
			return new ExternalAuthResult
			{
				Token = root.TryGetProperty("token", out var tk) ? tk.GetString() ?? string.Empty : string.Empty,
				SuccessMessage = root.TryGetProperty("successMessage", out var sm) ? sm.GetString() ?? string.Empty : string.Empty,
				Email = root.TryGetProperty("email", out var e) ? e.GetString() ?? string.Empty : string.Empty,
				UserName = root.TryGetProperty("userName", out var un) ? un.GetString() ?? string.Empty : string.Empty,
				Names = root.TryGetProperty("names", out var n) ? n.GetString() ?? string.Empty : string.Empty,
				Code = root.TryGetProperty("code", out var c) ? c.GetString() ?? string.Empty : string.Empty,
				Id = root.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var id) ? id : 0,
				EmployeeTypes = root.TryGetProperty("employeeTypes", out var et) ? et.GetString() ?? string.Empty : string.Empty
			};
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "External auth network error: {Message}", ex.Message);
			throw;
		}
		catch (TaskCanceledException ex)
		{
			_logger.LogError(ex, "External auth timed out: {Message}", ex.Message);
			throw new HttpRequestException("External authentication request timed out", ex);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "External auth unexpected error: {Message}", ex.Message);
			throw;
		}
	}
	
	private static string Truncate(string? text, int max = 512)
	{
		if (string.IsNullOrEmpty(text)) return string.Empty;
		return text.Length <= max ? text : text.Substring(0, max);
	}
}


