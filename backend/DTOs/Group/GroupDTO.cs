using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Group;

public class GroupDTO
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<UserDTO> Agents { get; set; } = new();
    // Optionally: add leader, manager, etc.
}