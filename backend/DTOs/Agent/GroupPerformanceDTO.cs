using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Agent;

public class GroupPerformanceDTO
{
    public string GroupName { get; set; } = string.Empty;
    public KPIs Kpis { get; set; } = new();
    public List<PerformanceTrend> PerformanceTrends { get; set; } = new();
    public TeamMember TeamLeader { get; set; } = new();
    public List<TeamMember> TeamMembers { get; set; } = new();
    public List<RecentActivity> RecentActivities { get; set; } = new();
    
    public class KPIs
    {
        public int? TotalClients { get; set; } = 0;
        public int? TeamMembersCount { get; set; }
        public TeamRank TeamRank { get; set; } = new();
    }
    
    public class TeamRank
    {
        public int? Rank { get; set; }
        public int? Total { get; set; }
    }
    
    public class PerformanceTrend
    {
        public string Name { get; set; } = string.Empty;
        public int? Clients { get; set; } = 0;
    }
    
    public class TeamMember
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? Clients { get; set; } = 0;
        public int? Rate { get; set; }
        public bool IsTeamLeader { get; set; }
    }
    
    public class RecentActivity
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }
}