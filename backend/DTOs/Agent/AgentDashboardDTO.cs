using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Agent;

public class AgentDashboardDTO
{
    public bool AttendanceMarked { get; set; }
    public int? ClientsThisMonth { get; set; } = 0;
    public int? TotalClients { get; set; } = 0;
    public double? PerformanceRate { get; set; }
    public List<RecentActivity> RecentActivities { get; set; } = new();

    // Only for sales agents
    public string? GroupName { get; set; }
    public string? TeamLeader { get; set; }
    
    public class RecentActivity
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }
}