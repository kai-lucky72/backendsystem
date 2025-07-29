using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Agent;

public class AgentListItemDTO
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string WorkId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public bool IsTeamLeader { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ClientsCollected { get; set; }
    public int AttendanceRate { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}