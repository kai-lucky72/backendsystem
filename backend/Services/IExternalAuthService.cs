using System.Threading;
using System.Threading.Tasks;

namespace backend.Services;

public interface IExternalAuthService
{
	Task<ExternalAuthResult?> AuthenticateAsync(string phoneNumber, string password, CancellationToken cancellationToken = default);
}

public sealed class ExternalAuthResult
{
	public string Token { get; init; } = string.Empty;
	public string SuccessMessage { get; init; } = string.Empty;
	public string Email { get; init; } = string.Empty;
	public string UserName { get; init; } = string.Empty;
	public string Names { get; init; } = string.Empty;
	public string Code { get; init; } = string.Empty;
	public int Id { get; init; }
	public string EmployeeTypes { get; init; } = string.Empty; // "Commercial" | "Sales Agent"
}

