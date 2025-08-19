using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace backend.Services;

public class ExternalAuthService : IExternalAuthService
{
	private readonly HttpClient _httpClient;
	private readonly string _authenticateUrl;

	public ExternalAuthService(HttpClient httpClient, IConfiguration configuration)
	{
		_httpClient = httpClient;
		var baseUrl = configuration["ExternalAuth:BaseUrl"]?.TrimEnd('/') ?? "";
		var path = configuration["ExternalAuth:AuthenticatePath"]?.Trim('/') ?? "User/api/IntermediariesAuthenticate";
		_authenticateUrl = string.IsNullOrWhiteSpace(baseUrl) ? path : $"{baseUrl}/{path}";
	}

	public async Task<ExternalAuthResult?> AuthenticateAsync(string phoneNumber, string password, CancellationToken cancellationToken = default)
	{
		var payload = new { userName = phoneNumber, password };
		using var response = await _httpClient.PostAsJsonAsync(_authenticateUrl, payload, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			return null;
		}

		var json = await response.Content.ReadAsStringAsync(cancellationToken);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;
		return new ExternalAuthResult
		{
			Token = root.GetProperty("token").GetString() ?? string.Empty,
			SuccessMessage = root.TryGetProperty("successMessage", out var sm) ? sm.GetString() ?? string.Empty : string.Empty,
			Email = root.TryGetProperty("email", out var e) ? e.GetString() ?? string.Empty : string.Empty,
			UserName = root.TryGetProperty("userName", out var un) ? un.GetString() ?? string.Empty : string.Empty,
			Names = root.TryGetProperty("names", out var n) ? n.GetString() ?? string.Empty : string.Empty,
			Code = root.TryGetProperty("code", out var c) ? c.GetString() ?? string.Empty : string.Empty,
			Id = root.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var id) ? id : 0,
			EmployeeTypes = root.TryGetProperty("employeeTypes", out var et) ? et.GetString() ?? string.Empty : string.Empty
		};
	}
}


