using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Manager;

public class ManagerListItemDTO
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AgentsCount { get; set; }
    public string LastLogin { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}